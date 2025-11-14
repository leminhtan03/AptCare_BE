using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.PayOSDto;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AptCare.Service.Services.PayOSService;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class TransactionService : BaseService<TransactionService>, ITransactionService
    {
        private readonly IUserContext _userContext;
        private readonly IS3FileService _s3FileService;
        private readonly IPayOSClient _payOSClient;
        private readonly PayOSOptions _payOSOptions;

        public TransactionService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IUserContext userContext, IS3FileService s3FileService, IPayOSClient payOSClient, IOptions<PayOSOptions> payOSOptions) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _payOSClient = payOSClient;
            _payOSOptions = payOSOptions.Value;
            _s3FileService = s3FileService;
        }

        public async Task<TransactionDto> CreateExpenseDepositAsync(TransactionExpenseDepositDto dto)
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
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

                if (invoice.Type != InvoiceType.ExternalContractor)
                    throw new AppValidationException("Chỉ hóa đơn nhà thầu mới được đặt cọc.", StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Không thể tạo giao dịch cho hóa đơn đã hủy.", StatusCodes.Status400BadRequest);

                if (dto.Amount <= 0)
                    throw new AppValidationException("Số tiền đặt cọc phải lớn hơn 0.", StatusCodes.Status400BadRequest);

                var totalExpenseList = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == dto.InvoiceId &&
                        t.Direction == TransactionDirection.Expense &&
                        t.Status == TransactionStatus.Success
                );
                var totalExpenseSum = totalExpenseList.Sum(t => (decimal?)t.Amount) ?? 0m;

                if (totalExpenseSum + dto.Amount > invoice.TotalAmount)
                    throw new AppValidationException("Tổng tiền chi (bao gồm lần cọc này) vượt quá giá trị hóa đơn.", StatusCodes.Status400BadRequest);

                if (dto.ContractorInvoiceFile == null || dto.ContractorInvoiceFile.Length == 0)
                    throw new AppValidationException("File hóa đơn nhà thầu là bắt buộc.", StatusCodes.Status400BadRequest);

                if (!dto.ContractorInvoiceFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    throw new AppValidationException("File hóa đơn phải là định dạng PDF.", StatusCodes.Status400BadRequest);

                var fileKey = await _s3FileService.UploadFileAsync(dto.ContractorInvoiceFile, $"transactions/invoices/{dto.InvoiceId}/");

                if (string.IsNullOrEmpty(fileKey))
                    throw new AppValidationException("Có lỗi xảy ra khi upload file hóa đơn.", StatusCodes.Status500InternalServerError);

                var depositCount = (await txRepo.GetListAsync(
                     predicate: t => t.InvoiceId == dto.InvoiceId &&
                        t.Direction == TransactionDirection.Expense &&
                        t.Description.StartsWith("Đặt cọc")
                )).Count;

                var depositIndex = depositCount + 1;

                var transaction = _mapper.Map<Transaction>(dto);
                transaction.UserId = _userContext.CurrentUserId;
                transaction.Description = $"Đặt cọc lần {depositIndex} cho nhà thầu #{dto.InvoiceId}" + (string.IsNullOrEmpty(dto.Note) ? "" : $" - {dto.Note}");

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();

                var media = new Media
                {
                    EntityId = transaction.TransactionId,
                    CreatedAt = DateTime.Now,
                    FileName = dto.ContractorInvoiceFile.FileName,
                    Entity = nameof(Transaction),
                    ContentType = dto.ContractorInvoiceFile.ContentType,
                    Status = ActiveStatus.Active,
                    FilePath = fileKey
                };
                await mediaRepo.InsertAsync(media);
                await _unitOfWork.CommitAsync();

                var newTotalExpense = totalExpenseSum + dto.Amount;
                if (newTotalExpense < invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }
                else if (newTotalExpense >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.PaidToContractor;
                }

                invoiceRepo.UpdateAsync(invoice);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created deposit transaction {TxId} for invoice {InvoiceId}", transaction.TransactionId, dto.InvoiceId);

                var result = _mapper.Map<TransactionDto>(transaction);
                result.AttachedFile = _mapper.Map<MediaDto>(media);
                result.UserFullName = await GetUserFullNameAsync(_userContext.CurrentUserId);

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating deposit transaction for invoice {InvoiceId}", dto.InvoiceId);
                throw;
            }
        }

        public async Task<TransactionDto> CreateExpenseFinalAsync(TransactionExpenseFinalDto dto)
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

                if (invoice.Type != InvoiceType.ExternalContractor)
                    throw new AppValidationException("Chỉ hóa đơn nhà thầu mới có thanh toán phần còn lại.", StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Không thể tạo giao dịch cho hóa đơn đã hủy.", StatusCodes.Status400BadRequest);

                var lastTracking = invoice.RepairRequest.RequestTrackings?.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status != RequestStatus.AcceptancePendingVerify || lastTracking?.Status != RequestStatus.Completed)
                    throw new AppValidationException("Công việc nhà thầu chưa hoàn tất, chưa thể thanh toán phần còn lại.", StatusCodes.Status400BadRequest);

                var hasFinalList = await txRepo.GetListAsync(
                    predicate: t =>
                        t.InvoiceId == dto.InvoiceId
                        && t.Direction == TransactionDirection.Expense
                        && t.Description.StartsWith("Thanh toán phần còn lại")
                );
                var hasFinal = hasFinalList.Any();

                if (hasFinal)
                    throw new AppValidationException("Đã tồn tại giao dịch thanh toán phần còn lại cho hóa đơn này.", StatusCodes.Status400BadRequest);

                var totalExpenseList = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == dto.InvoiceId
                        && t.Direction == TransactionDirection.Expense
                        && t.Status == TransactionStatus.Success
                );
                var totalExpense = totalExpenseList.Sum(t => (decimal?)t.Amount) ?? 0m;

                var remaining = invoice.TotalAmount - totalExpense;
                if (remaining <= 0)
                    throw new AppValidationException("Đã thanh toán đủ hoặc vượt quá, không còn phần chi nào cho nhà thầu.", StatusCodes.Status400BadRequest);

                if (dto.ContractorInvoiceFile == null || dto.ContractorInvoiceFile.Length == 0)
                    throw new AppValidationException("File hóa đơn nhà thầu là bắt buộc.", StatusCodes.Status400BadRequest);

                if (!dto.ContractorInvoiceFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    throw new AppValidationException("File hóa đơn phải là định dạng PDF.", StatusCodes.Status400BadRequest);

                var fileKey = await _s3FileService.UploadFileAsync(dto.ContractorInvoiceFile, $"transactions/invoices/{dto.InvoiceId}/");

                if (string.IsNullOrEmpty(fileKey))
                    throw new AppValidationException("Có lỗi xảy ra khi upload file hóa đơn.", StatusCodes.Status500InternalServerError);

                var transaction = _mapper.Map<Transaction>(dto);
                transaction.UserId = _userContext.CurrentUserId;
                transaction.Amount = remaining;
                transaction.Description = $"Thanh toán phần còn lại cho nhà thầu #{dto.InvoiceId}" + (string.IsNullOrEmpty(dto.Note) ? "" : $" - {dto.Note}");

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();

                var media = new Media
                {
                    EntityId = transaction.TransactionId,
                    CreatedAt = DateTime.Now,
                    FileName = dto.ContractorInvoiceFile.FileName,
                    Entity = nameof(Transaction),
                    ContentType = dto.ContractorInvoiceFile.ContentType,
                    Status = ActiveStatus.Active,
                    FilePath = fileKey
                };

                await mediaRepo.InsertAsync(media);
                await _unitOfWork.CommitAsync();

                invoice.Status = InvoiceStatus.PaidToContractor;
                invoiceRepo.UpdateAsync(invoice);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created final payment transaction {TxId} for invoice {InvoiceId}", transaction.TransactionId, dto.InvoiceId);

                var result = _mapper.Map<TransactionDto>(transaction);
                result.AttachedFile = _mapper.Map<MediaDto>(media);
                result.UserFullName = await GetUserFullNameAsync(transaction.UserId);

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating final payment transaction for invoice {InvoiceId}", dto.InvoiceId);
                throw;
            }
        }

        public async Task<TransactionDto> CreateExpenseInternalAsync(TransactionExpenseInternalDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var txRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == dto.InvoiceId
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

                if (invoice.Type != InvoiceType.InternalRepair)
                    throw new AppValidationException("Chỉ hóa đơn nội bộ mới có thể ghi chi phí nội bộ.", StatusCodes.Status400BadRequest);

                if (invoice.IsChargeable)
                    throw new AppValidationException("Hóa đơn này yêu cầu thu từ cư dân (IsChargeable = true). Không thể ghi chi phí nội bộ.", StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Không thể tạo giao dịch cho hóa đơn đã hủy.", StatusCodes.Status400BadRequest);

                var hasExpense = await txRepo.AnyAsync(t =>
                    t.InvoiceId == dto.InvoiceId
                    && t.Direction == TransactionDirection.Expense
                    && t.Status == TransactionStatus.Success);

                if (hasExpense)
                    throw new AppValidationException("Hóa đơn này đã có giao dịch chi phí nội bộ.", StatusCodes.Status400BadRequest);

                string? fileKey = null;
                Media? media = null;

                if (dto.ProofFile != null && dto.ProofFile.Length > 0)
                {
                    var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/jpg" };
                    if (!allowedTypes.Contains(dto.ProofFile.ContentType.ToLower()))
                        throw new AppValidationException("File chỉ được phép là PDF hoặc hình ảnh (JPEG, PNG).", StatusCodes.Status400BadRequest);

                    fileKey = await _s3FileService.UploadFileAsync(dto.ProofFile, $"transactions/invoices/{dto.InvoiceId}/");

                    if (string.IsNullOrEmpty(fileKey))
                        throw new AppValidationException("Có lỗi xảy ra khi upload file chứng từ.", StatusCodes.Status500InternalServerError);
                }

                var transaction = _mapper.Map<Transaction>(dto);
                transaction.UserId = _userContext.CurrentUserId;
                transaction.Amount = invoice.TotalAmount;
                transaction.Description = $"Chi phí nội bộ xuất kho #{dto.InvoiceId}"
                    + (string.IsNullOrEmpty(dto.Note) ? "" : $" - {dto.Note}");

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();

                if (!string.IsNullOrEmpty(fileKey) && dto.ProofFile != null)
                {
                    media = new Media
                    {
                        EntityId = transaction.TransactionId,
                        CreatedAt = DateTime.Now,
                        FileName = dto.ProofFile.FileName,
                        Entity = nameof(Transaction),
                        ContentType = dto.ProofFile.ContentType,
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

                _logger.LogInformation("Created internal expense transaction {TxId} for invoice {InvoiceId}",
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
                _logger.LogError(ex, "Error creating internal expense transaction for invoice {InvoiceId}", dto.InvoiceId);
                throw;
            }
        }

        public async Task<string> CreateIncomePaymentLinkAsync(int invoiceId)
        {
            try
            {
                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var txRepo = _unitOfWork.GetRepository<Transaction>();

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == invoiceId,
                    include: i => i.Include(x => x.RepairRequest)
                        .ThenInclude(r => r.RequestTrackings)
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

                if (!invoice.IsChargeable)
                    throw new AppValidationException(
                        "Hóa đơn này không thu phí từ cư dân (IsChargeable = false). Không thể tạo link PayOS.",
                        StatusCodes.Status400BadRequest);

                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Không thể tạo link thanh toán cho hóa đơn đã hủy.", StatusCodes.Status400BadRequest);

                if (invoice.TotalAmount <= 0)
                    throw new AppValidationException("Hóa đơn không có giá trị, không thể tạo thanh toán.", StatusCodes.Status400BadRequest);

                var lastTracking = invoice.RepairRequest.RequestTrackings?.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status != RequestStatus.Completed && lastTracking?.Status != RequestStatus.AcceptancePendingVerify)
                    throw new AppValidationException(
                        "Công việc sửa chữa chưa hoàn tất, chưa thể tạo link thanh toán cho cư dân.",
                        StatusCodes.Status400BadRequest);

                var totalIncome = await txRepo
                    .GetListAsync(
                        predicate: t => t.InvoiceId == invoiceId
                            && t.Direction == TransactionDirection.Income
                            && t.Status == TransactionStatus.Success
                    );
                var totalIncomeSum = totalIncome.Sum(t => (decimal?)t.Amount) ?? 0m;

                var remaining = invoice.TotalAmount - totalIncomeSum;
                if (remaining <= 0)
                    throw new AppValidationException("Hóa đơn đã được thanh toán đủ, không thể tạo thêm link.", StatusCodes.Status400BadRequest);

                var orderCode = GenerateOrderCode(invoiceId);

                // 5. Tạo Transaction Pending
                var transaction = new Transaction
                {
                    UserId = invoice.RepairRequest.UserId,
                    InvoiceId = invoiceId,
                    Direction = TransactionDirection.Income,
                    TransactionType = TransactionType.Payment,
                    Status = TransactionStatus.Pending,
                    Provider = PaymentProvider.PayOS,
                    Amount = remaining,
                    Description = $"Thanh toán hóa đơn sửa chữa #{invoiceId}",
                    CreatedAt = DateTime.UtcNow,
                    PayOSOrderCode = orderCode
                };

                await txRepo.InsertAsync(transaction);
                await _unitOfWork.CommitAsync();

                var (checkoutUrl, _) = await _payOSClient.CreatePaymentLinkAsync(
                      orderCode,
                      (long)Math.Round(remaining),
                      transaction.Description,
                      _payOSOptions.ReturnUrl);
                transaction.PayOSCheckoutUrl = checkoutUrl;
                txRepo.UpdateAsync(transaction);

                invoice.Status = InvoiceStatus.AwaitingPayment;
                invoiceRepo.UpdateAsync(invoice);

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Created PayOS link for invoice {InvoiceId}, OrderCode: {OrderCode}",
                    invoiceId, orderCode);

                return checkoutUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS link for invoice {InvoiceId}", invoiceId);
                throw;
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

                if (dto.Amount <= 0)
                    throw new AppValidationException("Số tiền thu phải lớn hơn 0.", StatusCodes.Status400BadRequest);

                var lastTracking = invoice.RepairRequest.RequestTrackings?.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status != RequestStatus.Completed && lastTracking?.Status != RequestStatus.AcceptancePendingVerify)
                    throw new AppValidationException("Công việc sửa chữa chưa hoàn tất, chưa thể thu tiền từ cư dân.", StatusCodes.Status400BadRequest);

                var totalIncomeList = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == dto.InvoiceId
                        && t.Direction == TransactionDirection.Income
                        && t.Status == TransactionStatus.Success
                );
                var totalIncomeSum = totalIncomeList.Sum(t => (decimal?)t.Amount) ?? 0m;

                if (totalIncomeSum + dto.Amount > invoice.TotalAmount)
                    throw new AppValidationException("Tổng tiền thu (bao gồm lần này) vượt quá giá trị hóa đơn.", StatusCodes.Status400BadRequest);

                string? fileKey = null;
                Media? media = null;

                if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0)
                {
                    var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/jpg" };
                    if (!allowedTypes.Contains(dto.ReceiptFile.ContentType.ToLower()))
                        throw new AppValidationException("File biên lai chỉ được phép là PDF hoặc hình ảnh (JPEG, PNG).", StatusCodes.Status400BadRequest);

                    fileKey = await _s3FileService.UploadFileAsync(
                        dto.ReceiptFile,
                        $"transactions/invoices/{dto.InvoiceId}/"
                    );

                    if (string.IsNullOrEmpty(fileKey))
                        throw new AppValidationException("Có lỗi xảy ra khi upload file biên lai.", StatusCodes.Status500InternalServerError);
                }

                var transaction = _mapper.Map<Transaction>(dto);
                transaction.UserId = _userContext.CurrentUserId;
                transaction.Description = $"Thu tiền mặt từ cư dân cho hóa đơn #{dto.InvoiceId}" + (string.IsNullOrEmpty(dto.Note) ? "" : $" - {dto.Note}");

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

                var newTotalIncome = totalIncomeSum + dto.Amount;
                if (newTotalIncome < invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }
                else if (newTotalIncome >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                }

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
                throw;
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

                Expression<Func<Transaction, bool>> predicate = t =>
                    (string.IsNullOrEmpty(search) || t.Description.ToLower().Contains(search))
                    && (!filterDto.InvoiceId.HasValue || t.InvoiceId == filterDto.InvoiceId.Value)
                    && (!filterDto.UserId.HasValue || t.UserId == filterDto.UserId.Value)
                    && (string.IsNullOrEmpty(filterDto.Direction) || t.Direction.ToString().ToLower() == filterDto.Direction.ToLower())
                    && (string.IsNullOrEmpty(filterDto.Status) || t.Status.ToString().ToLower() == filterDto.Status.ToLower())
                    && (string.IsNullOrEmpty(filterDto.Provider) || t.Provider.ToString().ToLower() == filterDto.Provider.ToLower())
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

        public async Task<(decimal TotalIncome, decimal TotalExpense)> GetInvoiceSummaryAsync(int invoiceId)
        {
            try
            {
                var txRepo = _unitOfWork.GetRepository<Transaction>();

                var totalIncomeList = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == invoiceId
                        && t.Direction == TransactionDirection.Income
                        && t.Status == TransactionStatus.Success
                );
                var totalIncome = totalIncomeList.Sum(t => (decimal?)t.Amount) ?? 0m;

                var totalExpenseList = await txRepo.GetListAsync(
                    predicate: t => t.InvoiceId == invoiceId
                        && t.Direction == TransactionDirection.Expense
                        && t.Status == TransactionStatus.Success
                );
                var totalExpense = totalExpenseList.Sum(t => (decimal?)t.Amount) ?? 0m;

                return (totalIncome, totalExpense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice summary for {InvoiceId}", invoiceId);
                throw;
            }
        }

        private long GenerateOrderCode(int invoiceId)
        {
            var now = DateTime.UtcNow;
            var prefix = long.Parse(now.ToString("yyMMddHHmmss"));
            return prefix * 1000 + (invoiceId % 1000);
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