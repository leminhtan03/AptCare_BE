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
        private const string INSPECTION_REPORT_TYPE = "InspectionReport";
        private const string REPAIR_REPORT_TYPE = "RepairReport";

        public ReportApprovalService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            IUserContext userContext,
            ILogger<ReportApprovalService> logger,
            IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
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

            var inspectionReport = await inspectionRepo.SingleOrDefaultAsync(
                predicate: ir => ir.InspectionReportId == dto.ReportId,
                include: i => i.Include(ir => ir.ReportApprovals)
                               .Include(ir => ir.Appointment)
                                   .ThenInclude(a => a.RepairRequest));

            if (inspectionReport == null)
            {
                throw new AppValidationException(
                    $"Không tìm thấy báo cáo kiểm tra. ReportId: {dto.ReportId}",
                    StatusCodes.Status404NotFound);
            }

            var currentApproval = (inspectionReport.ReportApprovals ?? Enumerable.Empty<ReportApproval>())
                .FirstOrDefault(ra => ra.UserId == userId && ra.Status == ReportStatus.Pending);

            if (currentApproval == null)
            {
                throw new AppValidationException("Không tìm thấy approval pending của bạn cho báo cáo này.", StatusCodes.Status404NotFound);
            }

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
                                    x.CreatedAt < inspectionReport.CreatedAt &&
                                    x.Status == InvoiceStatus.Draft &&
                                    x.Type != InvoiceType.AccessoryPurchase,
                    include: i => i.Include(x => x.InvoiceAccessories)
                                   .Include(x => x.InvoiceServices),
                    orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                );

                if (mainInvoice != null && inspectionReport.SolutionType != SolutionType.Outsource && mainInvoice.Type == InvoiceType.InternalRepair)
                {
                    if (dto.Status == ReportStatus.Approved)
                    {
                        mainInvoice.Status = InvoiceStatus.Approved;

                        var purchaseInvoice = await _unitOfWork.GetRepository<Invoice>().SingleOrDefaultAsync(
                            predicate: x => x.RepairRequestId == inspectionReport.Appointment.RepairRequestId &&
                                            x.Type == InvoiceType.AccessoryPurchase &&
                                            x.Status == InvoiceStatus.Draft &&
                                            x.CreatedAt >= minTime &&
                                            x.CreatedAt <= maxTime,
                            include: i => i.Include(x => x.InvoiceAccessories)
                                           .Include(x => x.InvoiceServices),
                            orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                        );

                        var purchasedAccessoryIds = purchaseInvoice?.InvoiceAccessories
                            .Where(a => a.AccessoryId.HasValue)
                            .Select(a => a.AccessoryId.Value)
                            .ToHashSet() ?? new HashSet<int>();

                        foreach (var invoiceAccessory in mainInvoice.InvoiceAccessories.Where(a => a.AccessoryId.HasValue))
                        {
                            if (purchasedAccessoryIds.Contains(invoiceAccessory.AccessoryId.Value))
                            {
                                continue;
                            }
                            var accessoryDb = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                                predicate: p => p.AccessoryId == invoiceAccessory.AccessoryId.Value &&
                                               p.Status == ActiveStatus.Active
                            );

                            if (accessoryDb == null)
                            {
                                throw new AppValidationException(
                                    $"Phụ kiện '{invoiceAccessory.Name}' không tồn tại.",
                                    StatusCodes.Status404NotFound);
                            }

                            if (accessoryDb.Quantity < invoiceAccessory.Quantity)
                            {
                                throw new AppValidationException(
                                    $"Phụ kiện '{invoiceAccessory.Name}' không đủ số lượng. " +
                                    $"Còn: {accessoryDb.Quantity}, Cần: {invoiceAccessory.Quantity}",
                                    StatusCodes.Status400BadRequest);
                            }

                            accessoryDb.Quantity -= invoiceAccessory.Quantity;
                            _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessoryDb);
                        }
                        if (purchaseInvoice != null)
                        {
                            
                            if (!mainInvoice.IsChargeable) {
                                var budget = await budgetRepo.SingleOrDefaultAsync();
                                if (budget == null)
                                {
                                    throw new AppValidationException("Không tìm thấy thông tin ngân sách.",
                                        StatusCodes.Status404NotFound);
                                }
                                if (budget.Amount < purchaseInvoice.TotalAmount)
                                {
                                    throw new AppValidationException(
                                        $"Ngân sách không đủ để mua phụ kiện.\n" +
                                        $"Cần: {purchaseInvoice.TotalAmount:N0} VNĐ\n" +
                                        $"Còn: {budget.Amount:N0} VNĐ\n" +
                                        $"Thiếu: {(purchaseInvoice.TotalAmount - budget.Amount):N0} VNĐ",
                                        StatusCodes.Status400BadRequest);
                                }

                                budget.Amount -= purchaseInvoice.TotalAmount;
                                budgetRepo.UpdateAsync(budget);

                                var purchaseDetails = string.Join(", ",
                                    purchaseInvoice.InvoiceAccessories.Select(a =>
                                        $"{a.Name} x{a.Quantity} x {a.Price:N0}đ"));

                                var transaction = new Transaction
                                {
                                    UserId = userId,
                                    InvoiceId = purchaseInvoice.InvoiceId,
                                    TransactionType = TransactionType.Cash,
                                    Status = TransactionStatus.Success,
                                    Provider = PaymentProvider.Budget,
                                    Direction = TransactionDirection.Expense,
                                    Amount = purchaseInvoice.TotalAmount,
                                    Description = $"Mua phụ kiện để sửa chữa ngay cho yêu cầu #{inspectionReport.Appointment.RepairRequestId}.\n" +
                                                 $"Chi tiết: {purchaseDetails}.\n" +
                                                 $"Phụ kiện được sử dụng trực tiếp, không nhập kho.\n" +
                                                 $"Người phê duyệt: {role}",
                                    CreatedAt = DateTime.Now,
                                    PaidAt = DateTime.Now
                                };

                                await transactionRepo.InsertAsync(transaction);
                            }

                            purchaseInvoice.Status = InvoiceStatus.Approved;
                            _unitOfWork.GetRepository<Invoice>().UpdateAsync(purchaseInvoice);
                        }

                        _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);
                    }
                    else if (dto.Status == ReportStatus.Rejected)
                    {
                        mainInvoice.Status = InvoiceStatus.Cancelled;

                        var purchaseInvoice = await _unitOfWork.GetRepository<Invoice>().SingleOrDefaultAsync(
                            predicate: x => x.RepairRequestId == inspectionReport.Appointment.RepairRequestId &&
                                            x.Type == InvoiceType.AccessoryPurchase &&
                                            x.Status == InvoiceStatus.Draft &&
                                            x.CreatedAt >= minTime &&
                                            x.CreatedAt <= maxTime
                        );

                        if (purchaseInvoice != null)
                        {
                            purchaseInvoice.Status = InvoiceStatus.Cancelled;
                            _unitOfWork.GetRepository<Invoice>().UpdateAsync(purchaseInvoice);
                        }

                        _unitOfWork.GetRepository<Invoice>().UpdateAsync(mainInvoice);
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
                                throw new AppValidationException("Không tìm thấy thông tin ngân sách.", StatusCodes.Status404NotFound);
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
                            var serviceDetails = string.Join(", ", mainInvoice.InvoiceServices?.Select(s => s.Name) ?? Enumerable.Empty<string>());
                            var accessoryDetails = string.Join(", ", mainInvoice.InvoiceAccessories?.Select(a => $"{a.Name} x{a.Quantity}") ?? Enumerable.Empty<string>());

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
                                             $"Phụ kiện: {accessoryDetails}\n" +
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
    }
}
