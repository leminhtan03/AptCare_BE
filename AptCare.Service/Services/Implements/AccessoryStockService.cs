using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class AccessoryStockService : BaseService<AccessoryStockService>, IAccessoryStockService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IS3FileService _s3FileService;

        public AccessoryStockService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<AccessoryStockService> logger, IUserContext userContext, ICloudinaryService cloudinaryService, IS3FileService s3FileService) : base(unitOfWork, logger, null)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _s3FileService = s3FileService;
        }

        public async Task<string> CreateStockInRequestAsync(StockInAccessoryDto dto)
        {
            int accessoryId = dto.AccessoryId ?? 0;
            Accessory? accessory = null;
            await _unitOfWork.BeginTransactionAsync();
            if (accessoryId > 0)
            {
                accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(predicate: x => x.AccessoryId == accessoryId);
                if (accessory == null)
                    throw new AppValidationException("Vật tư không tồn tại.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    throw new AppValidationException("Tên vật tư không được để trống.");
                bool isDup = await _unitOfWork.GetRepository<Accessory>().AnyAsync(x => x.Name == dto.Name);
                if (isDup)
                    throw new AppValidationException("Vật tư đã tồn tại, vui lòng chọn từ danh sách.");

                accessory = new Accessory
                {
                    Name = dto.Name!,
                    Descrption = dto.Description,
                    Price = dto.UnitPrice,
                    Quantity = 0,
                    Status = ActiveStatus.Active
                };
                await _unitOfWork.GetRepository<Accessory>().InsertAsync(accessory);
                await _unitOfWork.CommitAsync();
                accessoryId = accessory.AccessoryId;
            }

            var stockIn = new AccessoryStockTransaction
            {
                AccessoryId = accessoryId,
                Quantity = dto.Quantity,
                Type = StockTransactionType.Import,
                Status = StockTransactionStatus.Pending,
                Note = dto.Note,
                CreatedBy = _userContext.CurrentUserId,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.GetRepository<AccessoryStockTransaction>().InsertAsync(stockIn);
            await _unitOfWork.CommitAsync();
            await _unitOfWork.CommitTransactionAsync();
            return "Yêu cầu nhập kho đã được tạo thành công và đang chờ phê duyệt.";
        }

        public async Task<bool> ApproveStockInRequestAsync(int stockTransactionId, bool isApprove, string? note = null)
        {

            var repo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
            var stockIn = await repo.SingleOrDefaultAsync(predicate: x => x.StockTransactionId == stockTransactionId && x.Type == StockTransactionType.Import);
            if (stockIn == null || stockIn.Status != StockTransactionStatus.Pending)
                throw new AppValidationException("Yêu cầu nhập kho không hợp lệ hoặc đã được xử lý.");
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                stockIn.Status = isApprove ? StockTransactionStatus.Approved : StockTransactionStatus.Rejected;
                stockIn.ApprovedBy = _userContext.CurrentUserId;
                stockIn.ApprovedAt = DateTime.Now;
                stockIn.Note = note;

                if (isApprove)
                {
                    var transaction = new Transaction
                    {
                        UserId = stockIn.ApprovedBy.Value,
                        TransactionType = TransactionType.Cash,
                        Status = TransactionStatus.Success,
                        Provider = PaymentProvider.Budget,
                        Direction = TransactionDirection.Expense,
                        Amount = stockIn.TotalAmount.Value,
                        Description = $"Nhập kho vật tư {stockIn.AccessoryId}",
                        CreatedAt = DateTime.Now,
                        PaidAt = null
                    };
                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                    stockIn.TransactionId = transaction.TransactionId;

                    var budgetRepo = _unitOfWork.GetRepository<Budget>();
                    var budget = await budgetRepo.SingleOrDefaultAsync();
                    budget.Amount -= stockIn.TotalAmount.Value;
                    budgetRepo.UpdateAsync(budget);
                    await _unitOfWork.CommitAsync();

                }

                repo.UpdateAsync(stockIn);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi phê duyệt yêu cầu nhập kho.");
                throw;

            }
        }

        public async Task<int> CreateStockOutRequestAsync(int accessoryId, int quantity, int? repairRequestId, int? invoiceId, string note)
        {
            var stockOut = new AccessoryStockTransaction
            {
                AccessoryId = accessoryId,
                Quantity = quantity,
                Type = StockTransactionType.Export,
                Status = StockTransactionStatus.Pending,
                Note = note,
                CreatedBy = _userContext.CurrentUserId,
                CreatedAt = DateTime.Now,
                InvoiceId = invoiceId
            };
            await _unitOfWork.GetRepository<AccessoryStockTransaction>().InsertAsync(stockOut);
            await _unitOfWork.CommitAsync();
            return stockOut.StockTransactionId;
        }

        public async Task<bool> ApproveStockOutRequestAsync(int stockTransactionId)
        {
            var repo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
            var stockOut = await repo.SingleOrDefaultAsync(predicate: x => x.StockTransactionId == stockTransactionId && x.Type == StockTransactionType.Export);
            if (stockOut == null || stockOut.Status != StockTransactionStatus.Pending)
                throw new AppValidationException("Yêu cầu xuất kho không hợp lệ hoặc đã được xử lý.");

            // Kiểm tra tồn kho
            var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(predicate: x => x.AccessoryId == stockOut.AccessoryId);
            if (accessory.Quantity < stockOut.Quantity)
                throw new AppValidationException("Không đủ số lượng vật tư trong kho.");

            stockOut.Status = StockTransactionStatus.Approved;
            stockOut.ApprovedBy = _userContext.CurrentUserId;
            stockOut.ApprovedAt = DateTime.Now;

            accessory.Quantity -= stockOut.Quantity;
            _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);

            repo.UpdateAsync(stockOut);
            await _unitOfWork.CommitAsync();
            return true;
        }


        public async Task<bool> ConfirmStockInAsync(ConfirmStockInDto dto)
        {
            var repo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
            var stockIn = await repo.SingleOrDefaultAsync(predicate: x => x.StockTransactionId == dto.StockTransactionId && x.Type == StockTransactionType.Import);
            if (stockIn == null || stockIn.Status != StockTransactionStatus.Approved)
                throw new AppValidationException("Yêu cầu nhập kho không hợp lệ hoặc chưa được duyệt.");

            var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(predicate: x => x.AccessoryId == stockIn.AccessoryId);
            accessory.Quantity += stockIn.Quantity;
            _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);

            if (dto.VerificationFile != null && dto.VerificationFile.Length > 0)
            {
                string? filePath = null;
                var contentType = dto.VerificationFile.ContentType.ToLower();

                if (contentType.Contains("application/pdf"))
                {
                    filePath = await _s3FileService.UploadFileAsync(dto.VerificationFile, $"stock-in/{stockIn.StockTransactionId}/");
                }
                else if (contentType.Contains("image/jpeg") || contentType.Contains("image/png") || contentType.Contains("image/jpg"))
                {
                    filePath = await _cloudinaryService.UploadImageAsync(dto.VerificationFile);
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    await _unitOfWork.GetRepository<Media>().InsertAsync(new Media
                    {
                        Entity = nameof(AccessoryStockTransaction),
                        EntityId = stockIn.StockTransactionId,
                        FileName = dto.VerificationFile.FileName,
                        FilePath = filePath,
                        ContentType = dto.VerificationFile.ContentType,
                        CreatedAt = DateTime.Now,
                        Status = ActiveStatus.Active
                    });
                }
            }

            stockIn.Status = StockTransactionStatus.Completed;
            if (!string.IsNullOrWhiteSpace(dto.Note))
                stockIn.Note = dto.Note;
            var transaction = await _unitOfWork.GetRepository<Transaction>().SingleOrDefaultAsync(predicate: x => x.TransactionId == stockIn.TransactionId);
            transaction.PaidAt = DateTime.Now;
            transaction.Status = TransactionStatus.Success;
            _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);

            repo.UpdateAsync(stockIn);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IPaginate<AccessoryStockTransactionDto>> GetPaginateStockTransactionsAsync(StockTransactionFilterDto filter)
        {
            var page = filter.page > 0 ? filter.page : 1;
            var size = filter.size > 0 ? filter.size : 10;
            var search = filter.search?.ToLower() ?? string.Empty;
            var filterStr = filter.filter?.ToLower() ?? string.Empty;
            var sortBy = filter.sortBy?.ToLower() ?? string.Empty;

            StockTransactionStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filterStr))
            {
                if (Enum.TryParse<StockTransactionStatus>(filterStr, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            Expression<Func<AccessoryStockTransaction, bool>> predicate = x =>
                (string.IsNullOrEmpty(search)
                    || (x.Note != null && x.Note.ToLower().Contains(search))
                    || (x.Accessory != null && x.Accessory.Name.ToLower().Contains(search)))
                && (!filter.Type.HasValue || x.Type == filter.Type)
                && (!filter.Status.HasValue || x.Status == filter.Status)
                && (string.IsNullOrEmpty(filterStr) || (filterStatus.HasValue && x.Status == filterStatus.Value))
                && (!filter.FromDate.HasValue || x.CreatedAt >= filter.FromDate.Value.ToDateTime(TimeOnly.MinValue))
                && (!filter.ToDate.HasValue || x.CreatedAt <= filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue));

            Func<IQueryable<AccessoryStockTransaction>, IOrderedQueryable<AccessoryStockTransaction>> orderBy = q =>
            {
                return sortBy switch
                {
                    "createdat" => q.OrderBy(x => x.CreatedAt),
                    "createdat_desc" => q.OrderByDescending(x => x.CreatedAt),
                    "quantity" => q.OrderBy(x => x.Quantity),
                    "quantity_desc" => q.OrderByDescending(x => x.Quantity),
                    "status" => q.OrderBy(x => x.Status),
                    "status_desc" => q.OrderByDescending(x => x.Status),
                    "type" => q.OrderBy(x => x.Type),
                    "type_desc" => q.OrderByDescending(x => x.Type),
                    _ => q.OrderByDescending(x => x.CreatedAt)
                };
            };

            var result = await _unitOfWork.GetRepository<AccessoryStockTransaction>()
                .ProjectToPagingListAsync<AccessoryStockTransactionDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: predicate,
                    orderBy: orderBy,
                    page: page,
                    size: size
                );

            return result;
        }

        public async Task<AccessoryStockTransactionDto> GetStockTransactionByIdAsync(int stockTransactionId)
        {
            var transaction = await _unitOfWork.GetRepository<AccessoryStockTransaction>()
                .ProjectToSingleOrDefaultAsync<AccessoryStockTransactionDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: x => x.StockTransactionId == stockTransactionId
                );
            if (transaction == null)
                throw new AppValidationException("Không tìm thấy giao dịch xuất/nhập kho.", StatusCodes.Status404NotFound);

            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: m => _mapper.Map<MediaDto>(m),
                predicate: m => m.Entity == nameof(AccessoryStockTransaction)
                             && m.EntityId == stockTransactionId
                             && m.Status == ActiveStatus.Active
            );
            transaction.medias = medias.ToList();

            return transaction;
        }
    }
}