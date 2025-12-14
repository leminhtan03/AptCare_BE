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
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class AccessoryStockService : BaseService<AccessoryStockService>, IAccessoryStockService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IS3FileService _s3FileService;

        public AccessoryStockService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<AccessoryStockService> logger,
            IUserContext userContext,
            ICloudinaryService cloudinaryService,
            IS3FileService s3FileService,
            IMapper mapper) : base(unitOfWork, logger, mapper)
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
                    Status = ActiveStatus.Darft
                };
                await _unitOfWork.GetRepository<Accessory>().InsertAsync(accessory);
                await _unitOfWork.CommitAsync();
                accessoryId = accessory.AccessoryId;
            }

            var stockIn = new AccessoryStockTransaction
            {
                AccessoryId = accessoryId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                TotalAmount = dto.Quantity * dto.UnitPrice,
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
            var stockIn = await repo.SingleOrDefaultAsync(
                predicate: x => x.StockTransactionId == stockTransactionId && x.Type == StockTransactionType.Import,
                include: s => s.Include(a => a.Accessory)
            );

            if (stockIn == null || stockIn.Status != StockTransactionStatus.Pending)
                throw new AppValidationException("Yêu cầu nhập kho không hợp lệ hoặc đã được xử lý.");

            if (!stockIn.TotalAmount.HasValue)
                throw new AppValidationException("Tổng tiền không hợp lệ.");

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
                        UserId = _userContext.CurrentUserId,
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
                    await _unitOfWork.CommitAsync();

                    stockIn.TransactionId = transaction.TransactionId;

                    var budgetRepo = _unitOfWork.GetRepository<Budget>();
                    var budget = await budgetRepo.SingleOrDefaultAsync();

                    if (budget == null)
                        throw new AppValidationException("Không tìm thấy thông tin ngân sách.");

                    budget.Amount -= stockIn.TotalAmount.Value;
                    budgetRepo.UpdateAsync(budget);
                    await _unitOfWork.CommitAsync();

                    if (stockIn.Accessory == null)
                        throw new AppValidationException("Không tìm thấy thông tin vật tư.");
                    if (stockIn.Accessory.Status != ActiveStatus.Active)
                        stockIn.Accessory.Status = ActiveStatus.Active;

                    _unitOfWork.GetRepository<Accessory>().UpdateAsync(stockIn.Accessory);
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
        public async Task<bool> ApproveStockOutRequestAsync(int stockTransactionId, bool isApprove, string? note)
        {
            var repo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
            var stockOut = await repo.SingleOrDefaultAsync(
                predicate: x => x.StockTransactionId == stockTransactionId
                                && x.Type == StockTransactionType.Export);

            if (stockOut == null || stockOut.Status != StockTransactionStatus.Pending)
            {
                throw new AppValidationException("Yêu cầu xuất kho không hợp lệ hoặc đã được xử lý.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                stockOut.ApprovedBy = _userContext.CurrentUserId;
                stockOut.ApprovedAt = DateTime.Now;
                if (note != null)
                {
                    stockOut.Note = note;
                }
                if (isApprove)
                {
                    var accessoryRepo = _unitOfWork.GetRepository<Accessory>();
                    var accessory = await accessoryRepo.SingleOrDefaultAsync(
                        predicate: x => x.AccessoryId == stockOut.AccessoryId);

                    if (accessory == null)
                    {
                        throw new AppValidationException("Vật tư không tồn tại.");
                    }

                    if (accessory.Quantity < stockOut.Quantity)
                    {
                        throw new AppValidationException("Không đủ số lượng vật tư trong kho.");
                    }

                    accessory.Quantity -= stockOut.Quantity;
                    accessoryRepo.UpdateAsync(accessory);
                    stockOut.Status = StockTransactionStatus.Approved;
                }
                else
                {
                    stockOut.Status = StockTransactionStatus.Rejected;
                }

                repo.UpdateAsync(stockOut);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi phê duyệt yêu cầu xuất kho.");
                throw;
            }
        }


        public async Task<bool> ConfirmStockInAsync(ConfirmStockInDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var repo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
                var stockIn = await repo.SingleOrDefaultAsync(
                    predicate: x => x.StockTransactionId == dto.StockTransactionId && x.Type == StockTransactionType.Import,
                    include: s => s.Include(a => a.Invoice)
                                   .Include(a => a.Accessory)
                );

                if (stockIn == null)
                    throw new AppValidationException("Yêu cầu nhập kho không tồn tại.");

                if (stockIn.Status != StockTransactionStatus.Approved)
                    throw new AppValidationException("Yêu cầu nhập kho chưa được phê duyệt hoặc đã được xử lý.");

                var accessory = stockIn.Accessory;

                if (accessory == null)
                    throw new AppValidationException("Vật tư không tồn tại.");

                if (dto.IsConfirm)
                {
                    if (stockIn.Invoice == null)
                    {
                        accessory.Quantity += stockIn.Quantity;
                        _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                    }
                    else
                    {
                        if (stockIn.Invoice.Status == InvoiceStatus.Cancelled ||
                            stockIn.Invoice.Status == InvoiceStatus.Rejected)
                        {
                            accessory.Quantity += stockIn.Quantity;
                            _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                        }
                        //else if (stockIn.Invoice.Status == InvoiceStatus.Draft ||
                        //         stockIn.Invoice.Status == InvoiceStatus.Approved)
                        //{
                        //    accessory.Quantity += stockIn.Quantity;
                        //    _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                        //}
                        //else
                        //{
                        //    accessory.Quantity += stockIn.Quantity;
                        //    _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                        //}
                    }

                    if (dto.VerificationFile != null && dto.VerificationFile.Length > 0)
                    {
                        string? filePath = null;
                        var contentType = dto.VerificationFile.ContentType.ToLower();

                        if (contentType.Contains("application/pdf"))
                        {
                            filePath = await _s3FileService.UploadFileAsync(
                                dto.VerificationFile,
                                $"stock-in/{stockIn.StockTransactionId}/"
                            );
                        }
                        else if (contentType.Contains("image/jpeg") ||
                                 contentType.Contains("image/png") ||
                                 contentType.Contains("image/jpg"))
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
                        stockIn.Note += $"\n[Confirmed at {DateTime.Now:dd/MM/yyyy HH:mm}] {dto.Note}";

                    if (stockIn.TransactionId.HasValue)
                    {
                        var transaction = await _unitOfWork.GetRepository<Transaction>().SingleOrDefaultAsync(
                            predicate: x => x.TransactionId == stockIn.TransactionId
                        );

                        if (transaction != null)
                        {
                            transaction.PaidAt = DateTime.Now;
                            transaction.Status = TransactionStatus.Success;
                            _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                        }
                    }
                }
                else if (!dto.IsConfirm)
                {

                    if (stockIn.Invoice != null)
                    {
                        if (stockIn.Invoice.Status != InvoiceStatus.Cancelled || stockIn.Invoice.Status != InvoiceStatus.Rejected)
                        {
                            _logger.LogWarning(
                                "Yêu cầu nhập hàng #{StockInId} đang thực hiện cho yêu cầu sữa chữa #{RepairRequesrId} vui lòng kiểm tra lại khi hủy yêu cầu nhập hàng.",
                                stockIn.StockTransactionId,
                                stockIn.Invoice.RepairRequestId
                            );
                            throw new AppValidationException("Yêu cầu nhập hàng #{StockInId} đang thực hiện cho yêu cầu sữa chữa #{RepairRequesrId} vui lòng kiểm tra lại khi hủy yêu cầu nhập hàng.",
                                stockIn.StockTransactionId,
                                stockIn.Invoice.RepairRequestId);
                        }
                    }
                    stockIn.Status = StockTransactionStatus.Rejected;
                    stockIn.Note += dto.Note;

                    // Hoàn trả budget (vì đã trừ budget lúc approve)
                    if (stockIn.TransactionId.HasValue)
                    {
                        var transaction = await _unitOfWork.GetRepository<Transaction>().SingleOrDefaultAsync(
                            predicate: x => x.TransactionId == stockIn.TransactionId
                        );

                        if (transaction != null)
                        {
                            transaction.Status = TransactionStatus.Failed;
                            transaction.Description += $"\n[Rejected] Nhập kho bị từ chối, hoàn trả tiền.";
                            _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);

                            var budgetRepo = _unitOfWork.GetRepository<Budget>();
                            var budget = await budgetRepo.SingleOrDefaultAsync();
                            if (budget != null)
                            {
                                budget.Amount += stockIn.TotalAmount.Value;
                                budgetRepo.UpdateAsync(budget);
                            }
                        }
                    }
                }

                repo.UpdateAsync(stockIn);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error confirming/rejecting stock-in transaction {StockTransactionId}", dto.StockTransactionId);
                throw;
            }
        }
        public async Task<IPaginate<AccessoryStockTransactionDto>> GetPaginateStockTransactionsAsync(StockTransactionFilterDto filter)
        {
            var page = filter.page > 0 ? filter.page : 1;
            var size = filter.size > 0 ? filter.size : 10;
            var search = filter.search?.ToLower() ?? string.Empty;
            var filterStr = filter.filter?.ToLower() ?? string.Empty;

            StockTransactionStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filterStr) && Enum.TryParse<StockTransactionStatus>(filterStr, true, out var parsedStatus))
            {
                filterStatus = parsedStatus;
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

            var result = await _unitOfWork
                .GetRepository<AccessoryStockTransaction>()
                .ProjectToPagingListAsync<AccessoryStockTransactionDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: predicate,
                    include: s => s.Include(a => a.Accessory)
                                   .Include(c => c.CreatedByUser)
                                   .Include(a => a.ApprovedByUser),
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
                    predicate: x => x.StockTransactionId == stockTransactionId,
                    include: s => s.Include(a => a.Accessory)
                                   .Include(c => c.CreatedByUser)
                                   .Include(a => a.ApprovedByUser)
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

        public async Task<string> CreateStockOutRequestAsync(int id, StockOutAccessoryDto dto)
        {
            try
            {
                var stockRepo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
                var newStockOut = _mapper.Map<AccessoryStockTransaction>(dto);
                newStockOut.AccessoryId = id;
                newStockOut.CreatedBy = _userContext.CurrentUserId;
                await stockRepo.InsertAsync(newStockOut);
                await _unitOfWork.CommitAsync();
                return "Yêu cầu xuất kho đã được tạo thành công và đang chờ phê duyệt.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo yêu cầu xuất kho.");
                throw;
            }
        }

        public Task RevertStockForCancelledInvoiceAsync(int invoiceId)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> EnsureStockForInvoiceAsync(int invoiceId)
        {
            throw new NotImplementedException();
        }
    }
}