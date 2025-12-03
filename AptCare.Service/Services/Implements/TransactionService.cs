using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class TransactionService : BaseService<TransactionService>, ITransactionService
    {
        private readonly IUserContext _userContext;
        private readonly IS3FileService _s3FileService;
        private readonly PayOSOptions _payOSOptions;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly PayOSClient _payOS;

        public TransactionService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IUserContext userContext, IS3FileService s3FileService, ICloudinaryService cloudinaryService, IOptions<PayOSOptions> payOSOptions) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _s3FileService = s3FileService;
            _cloudinaryService = cloudinaryService;
            _payOSOptions = payOSOptions.Value;

            // Khởi tạo PayOS client với version 2.0.1
            _payOS = new PayOSClient(
                _payOSOptions.ClientId,
                _payOSOptions.ApiKey,
                _payOSOptions.ChecksumKey
            );
        }

        public async Task<string> CreateIncomePaymentLinkAsync(int invoiceId)
        {
            try
            {
                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var txRepo = _unitOfWork.GetRepository<Transaction>();
                await _unitOfWork.BeginTransactionAsync();

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == invoiceId,
                    include: i => i.Include(x => x.RepairRequest)
                                        .ThenInclude(r => r.RequestTrackings)
                                   .Include(x => x.InvoiceServices)
                                   .Include(x => x.InvoiceAccessories)
                                        .ThenInclude(ia => ia.Accessory)
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

                if (!invoice.IsChargeable)
                    throw new AppValidationException(
                        "Hóa đơn này không thu phí từ cư dân (IsChargeable = false).",
                        StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException(
                        "Không thể tạo link thanh toán cho hóa đơn đã hủy.",
                        StatusCodes.Status400BadRequest);

                if (invoice.TotalAmount <= 0)
                    throw new AppValidationException(
                        "Hóa đơn không có giá trị, không thể tạo thanh toán.",
                        StatusCodes.Status400BadRequest);

                if (invoice.TotalAmount < 2000)
                    throw new AppValidationException(
                        "Số tiền thanh toán tối thiểu là 2,000 VND.",
                        StatusCodes.Status400BadRequest);
                var lastTracking = invoice.RepairRequest.RequestTrackings?.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status != RequestStatus.Completed && lastTracking?.Status != RequestStatus.AcceptancePendingVerify)
                    throw new AppValidationException("Công việc sửa chữa chưa hoàn tất, chưa thể thu tiền từ cư dân.", StatusCodes.Status400BadRequest);

                var existingTransaction = await txRepo.SingleOrDefaultAsync(
                    predicate: t => t.InvoiceId == invoiceId
                        && t.Provider == PaymentProvider.PayOS
                        && t.Status == TransactionStatus.Pending
                );

                if (existingTransaction != null && !string.IsNullOrEmpty(existingTransaction.CheckoutUrl))
                {
                    _logger.LogInformation(
                        "PayOS transaction already exists for invoice {InvoiceId}, returning existing URL",
                        invoiceId);
                    return existingTransaction.CheckoutUrl;
                }

                var orderCode = GenerateOrderCode(invoiceId);

                var transaction = new Transaction
                {
                    UserId = invoice.RepairRequest.UserId,
                    InvoiceId = invoiceId,
                    TransactionType = TransactionType.Payment,
                    Status = TransactionStatus.Pending,
                    Provider = PaymentProvider.PayOS,
                    Amount = invoice.TotalAmount,
                    Description = $"Thanh toan hoa don #{invoiceId}",
                    CreatedAt = DateTime.Now,
                    OrderCode = orderCode
                };

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();
                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = (int)Math.Round(invoice.TotalAmount),
                    Description = transaction.Description,
                    Items = new List<PaymentLinkItem>(),
                    ReturnUrl = "https://your-url.com",
                    CancelUrl = "https://your-url.com"
                };
                if (invoice.InvoiceServices != null)
                {
                    foreach (var service in invoice.InvoiceServices)
                    {
                        paymentData.Items.Add(new PaymentLinkItem
                        {
                            Name = service.Name,
                            Quantity = 1,
                            Price = (int)Math.Round(service.Price),
                        });
                    }
                }

                if (invoice.InvoiceAccessories != null)
                {
                    foreach (var accessoryEntry in invoice.InvoiceAccessories)
                    {
                        var accessory = accessoryEntry.Accessory;
                        if (accessory != null)
                        {
                            paymentData.Items.Add(new PaymentLinkItem
                            {
                                Name = accessory.Name,
                                Quantity = accessoryEntry.Quantity,
                                Price = (int)Math.Round(accessory.Price),
                                Unit = "pcs"
                            });
                        }
                    }
                }

                _logger.LogInformation(
                     "Creating PayOS payment link - OrderCode: {OrderCode}, Amount: {Amount}, InvoiceId: {InvoiceId}",
                     orderCode, invoice.TotalAmount, invoiceId);

                var createPaymentResult = await _payOS.PaymentRequests.CreateAsync(paymentData);

                transaction.CheckoutUrl = createPaymentResult.CheckoutUrl;
                transaction.PayOSTransactionId = createPaymentResult.PaymentLinkId;
                txRepo.UpdateAsync(transaction);

                invoice.Status = InvoiceStatus.AwaitingPayment;
                invoiceRepo.UpdateAsync(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "PayOS payment link created - InvoiceId: {InvoiceId}, OrderCode: {OrderCode}, LinkId: {LinkId}",
                    invoiceId, orderCode, createPaymentResult.PaymentLinkId);

                return createPaymentResult.CheckoutUrl;
            }
            catch (Exception ex) when (ex.Message.Contains("order") && ex.Message.Contains("exist"))
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "PayOS OrderCode conflict for invoice {InvoiceId}", invoiceId);
                throw new AppValidationException(
                    "OrderCode đã tồn tại. Vui lòng thử lại." + ex.Message, StatusCodes.Status409Conflict);
            }
            catch (Exception ex) when (ex.Message.Contains("amount") || ex.Message.Contains("invalid"))
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "PayOS validation error for invoice {InvoiceId}", invoiceId);
                throw new AppValidationException(
                    "Thông tin thanh toán không hợp lệ. Vui lòng kiểm tra lại." + ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex) when (ex.Message.Contains("unauthorized") || ex.Message.Contains("credential"))
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "PayOS authentication error for invoice {InvoiceId}", invoiceId);
                throw new AppValidationException(
                    "Lỗi xác thực PayOS. Vui lòng liên hệ quản trị viên." + ex.Message,
                    StatusCodes.Status503ServiceUnavailable);
            }
            catch (AppValidationException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Unexpected error creating PayOS link for invoice {InvoiceId}", invoiceId);
                throw new AppValidationException(
                    "Có lỗi xảy ra khi tạo link thanh toán. Vui lòng thử lại sau." + ex.Message,
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<TransactionDto> CreateIncomeCashAsync(TransactionIncomeCashDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var txRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == dto.InvoiceId,
                    include: i => i.Include(x => x.RepairRequest)
                                     .ThenInclude(r => r.RequestTrackings)
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

                if (!invoice.IsChargeable)
                    throw new AppValidationException("Hóa đơn này không thu phí từ cư dân (IsChargeable = false).", StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Không thể tạo giao dịch cho hóa đơn đã hủy.", StatusCodes.Status400BadRequest);

                var lastTracking = invoice.RepairRequest.RequestTrackings?.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status != RequestStatus.Completed && lastTracking?.Status != RequestStatus.AcceptancePendingVerify)
                    throw new AppValidationException("Công việc sửa chữa chưa hoàn tất, chưa thể thu tiền từ cư dân.", StatusCodes.Status400BadRequest);

                string? fileKey = null;
                Media? media = null;
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };

                if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0 && dto.ReceiptFile.ContentType.ToLower().Contains("application/pdf"))
                {
                    fileKey = await _s3FileService.UploadFileAsync(dto.ReceiptFile, $"transactions/invoices/{dto.InvoiceId}/");
                    if (string.IsNullOrEmpty(fileKey))
                        throw new AppValidationException("Có lỗi xảy ra khi upload file biên lai.", StatusCodes.Status500InternalServerError);
                }
                else if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0 && allowedTypes.Contains(dto.ReceiptFile.ContentType.ToLower()))
                {
                    fileKey = await _cloudinaryService.UploadImageAsync(dto.ReceiptFile);
                }
                else if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0)
                {
                    throw new AppValidationException("Định dạng file biên lai không hợp lệ. Vui lòng tải lên file PDF hoặc hình ảnh.", StatusCodes.Status400BadRequest);
                }

                var transaction = _mapper.Map<Transaction>(dto);
                transaction.UserId = _userContext.CurrentUserId;
                transaction.Description = $"Thu tiền mặt từ cư dân cho hóa đơn #{dto.InvoiceId}" + (string.IsNullOrEmpty(dto.Note) ? "" : $" - {dto.Note}");
                transaction.Amount = invoice.TotalAmount;

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();

                if (!string.IsNullOrEmpty(fileKey) && dto.ReceiptFile != null)
                {
                    media = new Media
                    {
                        EntityId = transaction.TransactionId,
                        CreatedAt = DateTime.Now,
                        FileName = dto.ReceiptFile.FileName,
                        Entity = nameof(Transaction),
                        ContentType = dto.ReceiptFile.ContentType,
                        Status = ActiveStatus.Active,
                        FilePath = fileKey
                    };

                    await mediaRepo.InsertAsync(media);
                    await _unitOfWork.CommitAsync();
                }

                invoice.Status = InvoiceStatus.Paid;
                invoiceRepo.UpdateAsync(invoice);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created cash income transaction {TxId} for invoice {InvoiceId}",
                    transaction.TransactionId, dto.InvoiceId);

                var result = _mapper.Map<TransactionDto>(transaction);
                if (media != null)
                    result.AttachedFile = _mapper.Map<MediaDto>(media);
                result.UserFullName = await GetUserFullNameAsync(_userContext.CurrentUserId);

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating cash income transaction for invoice {InvoiceId}", dto.InvoiceId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByInvoiceIdAsync(int invoiceId)
        {
            try
            {
                var txRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                var transactions = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == invoiceId,
                    include: i => i.Include(t => t.User),
                    orderBy: q => q.OrderByDescending(t => t.CreatedAt)
                );

                var result = new List<TransactionDto>();

                foreach (var tx in transactions)
                {
                    var dto = _mapper.Map<TransactionDto>(tx);
                    dto.UserFullName = $"{tx.User.FirstName} {tx.User.LastName}";

                    var media = await mediaRepo.SingleOrDefaultAsync(
                        predicate: m => m.Entity == nameof(Transaction)
                            && m.EntityId == tx.TransactionId
                            && m.Status == ActiveStatus.Active
                    );

                    if (media != null)
                        dto.AttachedFile = _mapper.Map<MediaDto>(media);

                    result.Add(dto);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions by invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<TransactionDto> GetTransactionByIdAsync(int transactionId)
        {
            try
            {
                var txRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                var transaction = await txRepo.SingleOrDefaultAsync(
                    predicate: t => t.TransactionId == transactionId,
                    include: i => i.Include(t => t.User)
                );

                if (transaction == null)
                    throw new AppValidationException("Không tìm thấy giao dịch.", StatusCodes.Status404NotFound);

                var result = _mapper.Map<TransactionDto>(transaction);
                result.UserFullName = $"{transaction.User.FirstName} {transaction.User.LastName}";

                var media = await mediaRepo.SingleOrDefaultAsync(
                    predicate: m => m.Entity == nameof(Transaction)
                        && m.EntityId == transactionId
                        && m.Status == ActiveStatus.Active
                );

                if (media != null)
                    result.AttachedFile = _mapper.Map<MediaDto>(media);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TransactionId}", transactionId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<IPaginate<TransactionDto>> GetPaginateTransactionsAsync(TransactionFilterDto filterDto)
        {
            try
            {
                int page = filterDto.page > 0 ? filterDto.page : 1;
                int size = filterDto.size > 0 ? filterDto.size : 10;
                string search = filterDto.search?.ToLower() ?? string.Empty;

                var txRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();
                decimal? amountSearch = null;
                DateTime? dateSearch = null;
                if (!string.IsNullOrEmpty(search))
                {
                    if (decimal.TryParse(search, out var parsedAmount))
                        amountSearch = parsedAmount;
                    if (DateTime.TryParse(search, out var parsedDate))
                        dateSearch = parsedDate;
                }
                Expression<Func<Transaction, bool>> predicate = t =>
                    (string.IsNullOrEmpty(search)
                        || t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || (amountSearch.HasValue && t.Amount == amountSearch.Value)
                        || (dateSearch.HasValue && t.CreatedAt.Date == dateSearch.Value.Date)
                    )
                    && (!filterDto.InvoiceId.HasValue || t.InvoiceId == filterDto.InvoiceId.Value)
                    && (!filterDto.Direction.HasValue || t.Direction == filterDto.Direction.Value)
                    && (!filterDto.Status.HasValue || t.Status == filterDto.Status)
                    && (!filterDto.Provider.HasValue || t.Provider == filterDto.Provider)
                    && (!filterDto.TransactionType.HasValue || t.TransactionType == filterDto.TransactionType)
                    && (!filterDto.FromDate.HasValue || DateOnly.FromDateTime(t.CreatedAt) >= filterDto.FromDate.Value)
                    && (!filterDto.ToDate.HasValue || DateOnly.FromDateTime(t.CreatedAt) <= filterDto.ToDate.Value);

                var paginateResult = await txRepo.GetPagingListAsync(
                    page: page,
                    size: size,
                    predicate: predicate,
                    include: i => i.Include(t => t.User),
                    orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty),
                    selector: t => _mapper.Map<TransactionDto>(t)
                );

                foreach (var item in paginateResult.Items)
                {
                    var media = await mediaRepo.SingleOrDefaultAsync(
                        predicate: m => m.Entity == nameof(Transaction)
                            && m.EntityId == item.TransactionId
                            && m.Status == ActiveStatus.Active
                    );

                    if (media != null)
                        item.AttachedFile = _mapper.Map<MediaDto>(media);
                }

                return paginateResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginate transactions");
                throw;
            }
        }
        private long GenerateOrderCode(int invoiceId)
        {
            var now = DateTime.Now;
            var timestamp = ((DateTimeOffset)now).ToUnixTimeSeconds();
            var simpleOrderCode = timestamp * 1000 + (invoiceId % 1000);
            var orderCodeStr = simpleOrderCode.ToString();

            if (orderCodeStr.Length > 17)
            {
                simpleOrderCode = long.Parse(orderCodeStr[^17..]);
            }

            _logger.LogInformation("Generated OrderCode: {OrderCode} (Length: {Length}) for InvoiceId: {InvoiceId}",
                simpleOrderCode, simpleOrderCode.ToString().Length, invoiceId);

            return simpleOrderCode;
        }

        private Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(t => t.CreatedAt);

            return sortBy.ToLower() switch
            {
                "id" => q => q.OrderBy(t => t.TransactionId),
                "id_desc" => q => q.OrderByDescending(t => t.TransactionId),
                "date" => q => q.OrderBy(t => t.CreatedAt),
                "date_desc" => q => q.OrderByDescending(t => t.CreatedAt),
                "amount" => q => q.OrderBy(t => t.Amount),
                "amount_desc" => q => q.OrderByDescending(t => t.Amount),
                _ => q => q.OrderByDescending(t => t.CreatedAt)
            };
        }

        private async Task<string> GetUserFullNameAsync(int userId)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: u => $"{u.FirstName} {u.LastName}",
                predicate: u => u.UserId == userId
            );
            return user ?? "Unknown";
        }
    }
}