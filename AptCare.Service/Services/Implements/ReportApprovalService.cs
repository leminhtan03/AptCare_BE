using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class ReportApprovalService : BaseService<ReportApprovalService>, IReportApprovalService
    {
        private readonly IUserContext _userContext;
        private readonly IRedisCacheService _cacheService;
        private const string INSPECTION_REPORT_TYPE = "InspectionReport";
        private const string REPAIR_REPORT_TYPE = "RepairReport";

        public ReportApprovalService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            IUserContext userContext,
            ILogger<ReportApprovalService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cacheService = cacheService;
        }
        public async Task<bool> CreateApproveReportAsync(ApproveReportCreateDto dto)
        {
            try
            {
                if (dto.Status != ReportStatus.Pending)
                {
                    throw new AppValidationException("Chỉ có thể tạo approval với status Pending.", StatusCodes.Status400BadRequest);
                }

                var userId = _userContext.CurrentUserId;
                var role = Enum.Parse<AccountRole>(_userContext.Role);

                var (approverId, approverRole) = await GetNextApproverAsync(role);

                switch (dto.ReportType)
                {
                    case INSPECTION_REPORT_TYPE:
                        await CreateInspectionReportApprovalAsync(dto, approverId, approverRole);
                        break;

                    case REPAIR_REPORT_TYPE:
                        await CreateRepairReportApprovalAsync(dto, approverId, approverRole);
                        break;
                    default:
                        throw new AppValidationException(
                            $"Loại báo cáo không hợp lệ: {dto.ReportType}",
                            StatusCodes.Status400BadRequest);
                }
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo approval. ReportId: {ReportId}", dto.ReportId);
                throw new AppValidationException(ex.Message);
            }
        }
        public async Task<bool> ApproveReportAsync(ApproveReportCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;
                var role = Enum.Parse<AccountRole>(_userContext.Role);

                switch (dto.ReportType)
                {
                    case INSPECTION_REPORT_TYPE:
                        await ProcessInspectionReportApprovalAsync(dto, userId, role);
                        break;

                    case REPAIR_REPORT_TYPE:
                        await ProcessRepairReportApprovalAsync(dto, userId, role);
                        break;

                    default:
                        throw new AppValidationException(
                            $"Loại báo cáo không hợp lệ: {dto.ReportType}",
                            StatusCodes.Status400BadRequest);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi xử lý approval. ReportId: {ReportId}", dto.ReportId);
                throw new AppValidationException(ex.Message);
            }
        }


        private async Task CreateInspectionReportApprovalAsync(ApproveReportCreateDto dto, int approverId, AccountRole approverRole)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();

            var reportApproval = new ReportApproval
            {
                InspectionReportId = dto.ReportId,
                UserId = approverId,
                Role = approverRole,
                Status = ReportStatus.Pending,
                Comment = dto.Comment ?? "Chờ phê duyệt",
                CreatedAt = DateTime.Now
            };
            await reportApprovalRepo.InsertAsync(reportApproval);

        }

        private async Task ProcessInspectionReportApprovalAsync(ApproveReportCreateDto dto, int userId, AccountRole role)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
            var inspectionRepo = _unitOfWork.GetRepository<InspectionReport>();
            var budgetRepo = _unitOfWork.GetRepository<Budget>();
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var accessoryRepo = _unitOfWork.GetRepository<Accessory>();
            var stockTxRepo = _unitOfWork.GetRepository<AccessoryStockTransaction>();

            var inspectionReport = await inspectionRepo.SingleOrDefaultAsync(
                predicate: ir => ir.InspectionReportId == dto.ReportId,
                include: i => i.Include(ir => ir.ReportApprovals)
                               .Include(ir => ir.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
            );

            if (inspectionReport == null)
            {
                throw new AppValidationException($"Không tìm thấy báo cáo kiểm tra. ReportId: {dto.ReportId}",
                    StatusCodes.Status404NotFound);
            }

            var currentApproval = (inspectionReport.ReportApprovals ?? Enumerable.Empty<ReportApproval>())
                .FirstOrDefault(ra => ra.UserId == userId && ra.Status == ReportStatus.Pending);

            if (currentApproval == null)
                throw new AppValidationException("Không tìm thấy approval pending của bạn cho báo cáo này.",
                    StatusCodes.Status404NotFound);

            if (dto.EscalateToHigherLevel)
            {
                await EscalateApprovalAsync(currentApproval, dto, role);
                inspectionReport.Status = ReportStatus.Pending;
            }
            else
            {
                currentApproval.Status = dto.Status;
                currentApproval.Comment = dto.Comment;
                currentApproval.CreatedAt = DateTime.Now;

                reportApprovalRepo.UpdateAsync(currentApproval);
                inspectionReport.Status = dto.Status;

                var timeToleranceInSeconds = 5;
                var reportCreatedAt = inspectionReport.CreatedAt;
                var minTime = reportCreatedAt.AddSeconds(-timeToleranceInSeconds);
                var maxTime = reportCreatedAt.AddSeconds(timeToleranceInSeconds);

                var mainInvoice = await _unitOfWork.GetRepository<Invoice>().SingleOrDefaultAsync(
                    predicate: x => x.RepairRequestId == inspectionReport.Appointment.RepairRequestId &&
                                    x.CreatedAt >= minTime &&
                                    x.CreatedAt <= maxTime &&
                                    x.Status == InvoiceStatus.Draft,
                    include: i => i.Include(x => x.InvoiceAccessories)
                                   .Include(x => x.InvoiceServices),
                    orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                );

                if (mainInvoice != null && inspectionReport.SolutionType != SolutionType.Outsource)
                {
                    if (dto.Status == ReportStatus.Approved)
                    {
                        var accessoriesFromStock = mainInvoice.InvoiceAccessories
                            .Where(a => a.AccessoryId.HasValue &&
                                       a.SourceType == InvoiceAccessorySourceType.FromStock)
                            .ToList();

                        foreach (var invoiceAccessory in accessoriesFromStock)
                        {
                            var accessoryDb = await accessoryRepo.SingleOrDefaultAsync(
                                predicate: p => p.AccessoryId == invoiceAccessory.AccessoryId.Value &&
                                               p.Status == ActiveStatus.Active
                            );

                            if (accessoryDb == null)
                            {
                                throw new AppValidationException(
                                    $"Vật tư '{invoiceAccessory.Name}' không tồn tại.",
                                    StatusCodes.Status404NotFound);
                            }

                            if (accessoryDb.Quantity < invoiceAccessory.Quantity)
                            {
                                throw new AppValidationException(
                                    $"Vật tư '{invoiceAccessory.Name}' không đủ số lượng.\n" +
                                    $"Hiện có: {accessoryDb.Quantity}\n" +
                                    $"Cần: {invoiceAccessory.Quantity}\n" +
                                    $"Thiếu: {invoiceAccessory.Quantity - accessoryDb.Quantity}",
                                    StatusCodes.Status400BadRequest);
                            }
                            var stockOutTx = new AccessoryStockTransaction
                            {
                                AccessoryId = accessoryDb.AccessoryId,
                                InvoiceId = mainInvoice.InvoiceId,
                                Quantity = invoiceAccessory.Quantity,
                                UnitPrice = invoiceAccessory.Price,
                                TotalAmount = invoiceAccessory.Price * invoiceAccessory.Quantity,
                                Type = StockTransactionType.Export,
                                Status = StockTransactionStatus.Approved,
                                Note = $"Xuất kho tự động cho Invoice #{mainInvoice.InvoiceId} - RepairRequest #{inspectionReport.Appointment.RepairRequestId}",
                                CreatedBy = userId,
                                CreatedAt = DateTime.Now,
                                ApprovedBy = userId,
                                ApprovedAt = DateTime.Now
                            };

                            await stockTxRepo.InsertAsync(stockOutTx);

                            accessoryDb.Quantity -= invoiceAccessory.Quantity;
                            accessoryRepo.UpdateAsync(accessoryDb);

                            await _cacheService.RemoveAsync($"accessory:{accessoryDb.AccessoryId}");
                        }

                        await _cacheService.RemoveByPrefixAsync("accessory:list");
                        await _cacheService.RemoveByPrefixAsync("accessory:paginate");

                        // ✅ XỬ LÝ VẬT TƯ CẦN MUA (ToBePurchased) - TẠO PHIẾU NHẬP
                        var accessoriesToPurchase = mainInvoice.InvoiceAccessories
                            .Where(a => a.AccessoryId.HasValue &&
                                       a.SourceType == InvoiceAccessorySourceType.ToBePurchased)
                            .ToList();

                        if (accessoriesToPurchase.Any())
                        {
                            var budget = await budgetRepo.SingleOrDefaultAsync();
                            if (budget == null)
                            {
                                throw new AppValidationException("Không tìm thấy thông tin ngân sách.",
                                    StatusCodes.Status404NotFound);
                            }

                            decimal totalPurchaseAmount = accessoriesToPurchase.Sum(a => a.Price * a.Quantity);

                            if (budget.Amount < totalPurchaseAmount)
                            {
                                throw new AppValidationException(
                                    $"Ngân sách không đủ để mua vật tư.\n" +
                                    $"Cần: {totalPurchaseAmount:N0} VNĐ\n" +
                                    $"Còn: {budget.Amount:N0} VNĐ\n" +
                                    $"Thiếu: {(totalPurchaseAmount - budget.Amount):N0} VNĐ",
                                    StatusCodes.Status400BadRequest);
                            }

                            var purchaseDetails = new List<string>();
                            foreach (var invoiceAccessory in accessoriesToPurchase)
                            {
                                var stockInTx = new AccessoryStockTransaction
                                {
                                    AccessoryId = invoiceAccessory.AccessoryId.Value,
                                    InvoiceId = mainInvoice.InvoiceId,
                                    Quantity = invoiceAccessory.Quantity,
                                    UnitPrice = invoiceAccessory.Price,
                                    TotalAmount = invoiceAccessory.Price * invoiceAccessory.Quantity,
                                    Type = StockTransactionType.Import,
                                    Status = StockTransactionStatus.Approved,
                                    Note = $"Nhập kho tự động cho Invoice #{mainInvoice.InvoiceId} - Vật tư mua mới để sử dụng ngay",
                                    CreatedBy = userId,
                                    CreatedAt = DateTime.Now,
                                    ApprovedBy = userId,
                                    ApprovedAt = DateTime.Now
                                };

                                await stockTxRepo.InsertAsync(stockInTx);
                                purchaseDetails.Add($"{invoiceAccessory.Name} x{invoiceAccessory.Quantity} x {invoiceAccessory.Price:N0}đ");

                                var accessoryDb = await accessoryRepo.SingleOrDefaultAsync(
                                    predicate: a => a.AccessoryId == invoiceAccessory.AccessoryId.Value
                                );

                                if (accessoryDb != null && accessoryDb.Status == ActiveStatus.Darft)
                                {
                                    accessoryDb.Status = ActiveStatus.Active;
                                    accessoryRepo.UpdateAsync(accessoryDb);
                                    _logger.LogInformation("Vật tư mới '{AccessoryName}' (ID: {AccessoryId}) được kích hoạt sau khi approve inspection.",
                                        accessoryDb.Name,
                                        accessoryDb.AccessoryId);
                                }
                            }

                            budget.Amount -= totalPurchaseAmount;
                            budgetRepo.UpdateAsync(budget);

                            var transaction = new Transaction
                            {
                                UserId = userId,
                                TransactionType = TransactionType.Cash,
                                Status = TransactionStatus.Success,
                                Provider = PaymentProvider.Budget,
                                Direction = TransactionDirection.Expense,
                                Amount = totalPurchaseAmount,
                                Description = $"Mua vật tư để sửa chữa ngay cho yêu cầu #{inspectionReport.Appointment.RepairRequestId}.\n" +
                                             $"Chi tiết: {string.Join(", ", purchaseDetails)}.\n" +
                                             $"Vật tư được sử dụng trực tiếp, không nhập kho ngay.\n" +
                                             $"Người phê duyệt: {role}",
                                CreatedAt = DateTime.Now,
                                PaidAt = null
                            };
                            await transactionRepo.InsertAsync(transaction);

                            if (accessoriesToPurchase.Any())
                            {
                                var firstStockIn = await stockTxRepo.SingleOrDefaultAsync(
                                    predicate: st => st.InvoiceId == mainInvoice.InvoiceId &&
                                                    st.Type == StockTransactionType.Import,
                                    orderBy: o => o.OrderBy(st => st.CreatedAt)
                                );

                                if (firstStockIn != null)
                                {
                                    firstStockIn.TransactionId = transaction.TransactionId;
                                    stockTxRepo.UpdateAsync(firstStockIn);
                                }
                            }
                        }

                        mainInvoice.Status = InvoiceStatus.Approved;
                        _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);

                        _logger.LogInformation(
                            "Approved inspection #{InspectionId} - Created {ExportCount} export transactions and {ImportCount} import transactions",
                            inspectionReport.InspectionReportId,
                            accessoriesFromStock.Count,
                            accessoriesToPurchase.Count);
                    }
                    else if (dto.Status == ReportStatus.Rejected)
                    {
                        mainInvoice.Status = InvoiceStatus.Cancelled;
                        _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);

                        _logger.LogWarning(
                            "Rejected inspection #{InspectionId} - Invoice #{InvoiceId} cancelled",
                            inspectionReport.InspectionReportId,
                            mainInvoice.InvoiceId);
                    }
                }

                if (mainInvoice != null && inspectionReport.SolutionType == SolutionType.Outsource && mainInvoice.Type == InvoiceType.ExternalContractor)
                {
                    if (dto.Status == ReportStatus.Approved)
                    {
                        if (!mainInvoice.IsChargeable)
                        {
                            var budget = await budgetRepo.SingleOrDefaultAsync();
                            if (budget == null)
                            {
                                throw new AppValidationException("Không tìm thấy thông tin ngân sách.",
                                    StatusCodes.Status404NotFound);
                            }

                            if (budget.Amount < mainInvoice.TotalAmount)
                            {
                                throw new AppValidationException(
                                    $"Ngân sách không đủ để thuê nhà thầu.\n" +
                                    $"Cần: {mainInvoice.TotalAmount:N0} VNĐ\n" +
                                    $"Còn: {budget.Amount:N0} VNĐ\n" +
                                    $"Thiếu: {(mainInvoice.TotalAmount - budget.Amount):N0} VNĐ",
                                    StatusCodes.Status400BadRequest);
                            }

                            budget.Amount -= mainInvoice.TotalAmount;
                            budgetRepo.UpdateAsync(budget);

                            var serviceDetails = string.Join(", ",
                                mainInvoice.InvoiceServices?.Select(s => s.Name) ?? Enumerable.Empty<string>());
                            var accessoryDetails = string.Join(", ",
                                mainInvoice.InvoiceAccessories?.Select(a => $"{a.Name} x{a.Quantity}") ?? Enumerable.Empty<string>());

                            var transaction = new Transaction
                            {
                                UserId = userId,
                                InvoiceId = mainInvoice.InvoiceId,
                                TransactionType = TransactionType.Cash,
                                Status = TransactionStatus.Pending,
                                Provider = PaymentProvider.Budget,
                                Direction = TransactionDirection.Expense,
                                Amount = mainInvoice.TotalAmount,
                                Description = $"Cam kết thanh toán cho nhà thầu (Invoice #{mainInvoice.InvoiceId}).\n" +
                                             $"Dịch vụ: {serviceDetails}\n" +
                                             $"Vật tư: {accessoryDetails}\n" +
                                             $"Trạng thái: Đã dành tiền, chờ thanh toán thực tế.\n" +
                                             $"Người phê duyệt: {role}",
                                CreatedAt = DateTime.Now,
                                PaidAt = null
                            };

                            await transactionRepo.InsertAsync(transaction);

                            mainInvoice.Status = InvoiceStatus.Approved;
                            _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);
                        }
                        else
                        {
                            mainInvoice.Status = InvoiceStatus.AwaitingPayment;
                            _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);
                        }
                    }
                    else if (dto.Status == ReportStatus.Rejected)
                    {

                        mainInvoice.Status = InvoiceStatus.Cancelled;
                        _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);
                    }
                }
            }

            inspectionRepo.UpdateAsync(inspectionReport);
        }
        private async Task CreateRepairReportApprovalAsync(ApproveReportCreateDto dto, int approverId, AccountRole approverRole)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
            var repairRepo = _unitOfWork.GetRepository<RepairReport>();
            var exists = await repairRepo.AnyAsync(
                predicate: rr => rr.RepairReportId == dto.ReportId);

            if (!exists)
            {
                throw new AppValidationException(
                    $"Không tìm thấy báo cáo sửa chữa. ReportId: {dto.ReportId}",
                    StatusCodes.Status404NotFound);
            }

            var reportApproval = new ReportApproval
            {
                RepairReportId = dto.ReportId,
                UserId = approverId,
                Role = approverRole,
                Status = ReportStatus.Pending,
                Comment = dto.Comment ?? "Chờ phê duyệt",
                CreatedAt = DateTime.Now
            };

            await reportApprovalRepo.InsertAsync(reportApproval);
        }

        private async Task ProcessRepairReportApprovalAsync(ApproveReportCreateDto dto, int userId, AccountRole role)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
            var repairRepo = _unitOfWork.GetRepository<RepairReport>();

            var repairReport = await repairRepo.SingleOrDefaultAsync(
                predicate: rr => rr.RepairReportId == dto.ReportId,
                include: i => i.Include(rr => rr.ReportApprovals)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.AppointmentAssigns)
                                  );

            if (repairReport == null)
            {
                throw new AppValidationException(
                    $"Không tìm thấy báo cáo sửa chữa. ReportId: {dto.ReportId}",
                    StatusCodes.Status404NotFound);
            }

            var currentApproval = (repairReport.ReportApprovals ?? Enumerable.Empty<ReportApproval>())
                .FirstOrDefault(ra => ra.UserId == userId && ra.Status == ReportStatus.Pending);

            if (currentApproval == null)
            {
                throw new AppValidationException(
                    "Không tìm thấy approval pending của bạn cho báo cáo này.",
                    StatusCodes.Status404NotFound);
            }

            if (dto.EscalateToHigherLevel)
            {
                await EscalateApprovalAsync(currentApproval, dto, role);
                repairReport.Status = ReportStatus.Pending;
            }
            else
            {
                currentApproval.Status = dto.Status;
                currentApproval.Comment = dto.Comment;
                currentApproval.CreatedAt = DateTime.Now;

                reportApprovalRepo.UpdateAsync(currentApproval);
                repairReport.Status = dto.Status;
            }
            repairRepo.UpdateAsync(repairReport);
        }

        private async Task<(int userId, AccountRole role)> GetNextApproverAsync(AccountRole currentRole)
        {
            var userRepo = _unitOfWork.GetRepository<User>();

            switch (currentRole)
            {
                case AccountRole.Technician:
                    var techLead = await userRepo.SingleOrDefaultAsync(
                        selector: u => u.UserId,
                        predicate: u => u.Account.Role == AccountRole.TechnicianLead,
                        include: i => i.Include(u => u.Account));

                    if (techLead == 0)
                        throw new AppValidationException("Không tìm thấy TechnicianLead trong hệ thống.");

                    return (techLead, AccountRole.TechnicianLead);

                case AccountRole.TechnicianLead:
                    var manager = await userRepo.SingleOrDefaultAsync(
                        selector: u => u.UserId,
                        predicate: u => u.Account.Role == AccountRole.Manager,
                        include: i => i.Include(u => u.Account));
                    if (manager == 0)
                        throw new AppValidationException("Không tìm thấy Manager trong hệ thống.");
                    return (manager, AccountRole.Manager);
                case AccountRole.Manager:
                    var Admin = await userRepo.SingleOrDefaultAsync(
                        selector: u => u.UserId,
                        predicate: u => u.Account.Role == AccountRole.Admin,
                        include: i => i.Include(u => u.Account));
                    if (Admin == 0)
                        throw new AppValidationException("Không tìm thấy Director trong hệ thống.");
                    return (Admin, AccountRole.Admin);
                default:
                    throw new AppValidationException(
                        $"Role {currentRole} không được phép tạo approval.",
                        StatusCodes.Status403Forbidden);
            }
        }
        private async Task EscalateApprovalAsync(ReportApproval currentApproval, ApproveReportCreateDto dto, AccountRole currentRole)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();

            // Đánh dấu approval hiện tại là Approved
            currentApproval.Status = ReportStatus.Approved;
            currentApproval.Comment = dto.Comment ?? "Đã phê duyệt và chuyển lên cấp cao hơn";
            currentApproval.CreatedAt = DateTime.Now;
            reportApprovalRepo.UpdateAsync(currentApproval);

            // Tạo approval mới cho cấp cao hơn
            var (nextApproverId, nextApproverRole) = await GetNextApproverAsync(currentRole);

            var newApproval = new ReportApproval
            {
                InspectionReportId = currentApproval.InspectionReportId != 0 ? currentApproval.InspectionReportId : null,
                RepairReportId = currentApproval.RepairReportId != 0 ? currentApproval.RepairReportId : null,
                UserId = nextApproverId,
                Role = nextApproverRole,
                Status = ReportStatus.Pending,
                Comment = $"Chuyển từ {currentRole} - {dto.Comment}",
                CreatedAt = DateTime.Now
            };

            await reportApprovalRepo.InsertAsync(newApproval);
        }

        public async Task<bool> ResidentApproveRepairReportAsync(int repairReportId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
                var repairRepo = _unitOfWork.GetRepository<RepairReport>();

                var repairReport = await repairRepo.SingleOrDefaultAsync(
                    predicate: rr => rr.RepairReportId == repairReportId,
                    include: i => i.Include(rr => rr.Appointment)
                                       .ThenInclude(a => a.RepairRequest)
                                  );
                if (repairReport == null)
                {
                    throw new AppValidationException(
                        $"Không tìm thấy báo cáo sửa chữa. ReportId: {repairReportId}",
                        StatusCodes.Status404NotFound);
                }

                var residentApproval = await reportApprovalRepo.SingleOrDefaultAsync(
                    predicate: ra => ra.Role == AccountRole.Resident && ra.Status == ReportStatus.Pending && ra.RepairReportId == repairReportId
                    );
                if (residentApproval == null)
                {
                    throw new AppValidationException(
                        "Không tìm thấy resident approval pending của bạn cho báo cáo này.",
                        StatusCodes.Status404NotFound);
                }
                var isValidResident = await _unitOfWork.GetRepository<UserApartment>().AnyAsync(
                    predicate: p => p.ApartmentId == repairReport.Appointment.RepairRequest.ApartmentId && 
                                    p.UserId == userId && p.Status == ActiveStatus.Active
                    );
                if (!isValidResident)
                {
                    throw new AppValidationException($"Cư dân không thuộc căn hộ của yêu cầu sửa chữa này.");
                }

                residentApproval.Status = ReportStatus.ResidentApproved;
                repairRepo.UpdateAsync(repairReport);
                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý resident approval. ReportId: {ReportId}", repairReportId);
                throw new AppValidationException(ex.Message);
            }
        }

        public async Task<bool> CheckResidentApproveRepairReportAsync(int repairReportId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
                var repairRepo = _unitOfWork.GetRepository<RepairReport>();

                var repairReport = await repairRepo.SingleOrDefaultAsync(
                    predicate: rr => rr.RepairReportId == repairReportId,
                    include: i => i.Include(rr => rr.Appointment)
                                       .ThenInclude(a => a.RepairRequest)
                                  );
                if (repairReport == null)
                {
                    throw new AppValidationException(
                        $"Không tìm thấy báo cáo sửa chữa. ReportId: {repairReportId}",
                        StatusCodes.Status404NotFound);
                }

                var residentApproval = await reportApprovalRepo.SingleOrDefaultAsync(
                    predicate: ra => ra.Role == AccountRole.Resident && ra.RepairReportId == repairReportId
                    );
                if (residentApproval == null)
                {
                    throw new AppValidationException(
                        "Không tìm thấy resident approval pending của bạn cho báo cáo này.",
                        StatusCodes.Status404NotFound);
                }
                if (residentApproval.Status == ReportStatus.Pending)
                {
                    return false;
                }
                if (residentApproval.Status == ReportStatus.ResidentApproved)
                {
                    return true;
                }

                throw new AppValidationException("Something went wrong");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý resident approval. ReportId: {ReportId}", repairReportId);
                throw new AppValidationException(ex.Message);
            }
        }
    }
}
