using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ApproveReportDtos;
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

        /// <summary>
        /// Tạo approval pending cho TechnicianLead khi Technician tạo báo cáo
        /// </summary>
        public async Task<bool> CreateApproveReportAsync(ApproveReportCreateDto dto)
        {
            try
            {
                if (dto.Status != ReportStatus.Pending)
                {
                    throw new AppValidationException(
                        "Chỉ có thể tạo approval với status Pending.",
                        StatusCodes.Status400BadRequest);
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
                throw;
            }
        }

        /// <summary>
        /// Xử lý approve/reject hoặc escalate lên cấp cao hơn
        /// </summary>
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
                throw;
            }
        }

        #region InspectionReport Processing

        private async Task CreateInspectionReportApprovalAsync(
            ApproveReportCreateDto dto,
            int approverId,
            AccountRole approverRole)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();

            var reportApproval = new ReportApproval
            {
                InspectionReportId = dto.ReportId,
                UserId = approverId,
                Role = approverRole,
                Status = ReportStatus.Pending,
                Comment = dto.Comment ?? "Chờ phê duyệt",
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await reportApprovalRepo.InsertAsync(reportApproval);

        }

        private async Task ProcessInspectionReportApprovalAsync(
            ApproveReportCreateDto dto,
            int userId,
            AccountRole role)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
            var inspectionRepo = _unitOfWork.GetRepository<InspectionReport>();

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

            var currentApproval = inspectionReport.ReportApprovals?
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
                inspectionReport.Status = ReportStatus.Pending;
            }
            else
            {
                currentApproval.Status = dto.Status;
                currentApproval.Comment = dto.Comment;
                currentApproval.CreatedAt = DateTime.UtcNow.AddHours(7);

                reportApprovalRepo.UpdateAsync(currentApproval);
                inspectionReport.Status = dto.Status;

                if (dto.Status == ReportStatus.Approved && inspectionReport.Appointment?.RepairRequest != null)
                {
                    var repairRequest = inspectionReport.Appointment.RepairRequest;

                    var requestTracking = new RequestTracking
                    {
                        RepairRequestId = repairRequest.RepairRequestId,
                        Status = RequestStatus.CompletedPendingVerify,
                        UpdatedAt = DateTime.UtcNow.AddHours(7),
                        UpdatedBy = userId,
                        Note = "Báo cáo kiểm tra đã được phê duyệt, đã xác định phạm vi sửa chữa."
                    };
                    await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(requestTracking);
                }
            }

            inspectionRepo.UpdateAsync(inspectionReport);
        }
        #endregion

        #region RepairReport Processing

        private async Task CreateRepairReportApprovalAsync(
            ApproveReportCreateDto dto,
            int approverId,
            AccountRole approverRole)
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
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await reportApprovalRepo.InsertAsync(reportApproval);
        }

        private async Task ProcessRepairReportApprovalAsync(
            ApproveReportCreateDto dto,
            int userId,
            AccountRole role)
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

            var currentApproval = repairReport.ReportApprovals?
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
                currentApproval.CreatedAt = DateTime.UtcNow.AddHours(7);

                reportApprovalRepo.UpdateAsync(currentApproval);
                repairReport.Status = dto.Status;

                if (dto.Status == ReportStatus.Approved && repairReport.Appointment?.RepairRequest != null)
                {
                    var repairRequest = repairReport.Appointment.RepairRequest;

                    var appointment = repairReport.Appointment;

                    var appointmentTracking = new AppointmentTracking
                    {
                        AppointmentId = appointment.AppointmentId,
                        Status = AppointmentStatus.Completed,
                        UpdatedAt = DateTime.UtcNow.AddHours(7),
                        UpdatedBy = userId,
                        Note = "Báo cáo sửa chữa đã được phê duyệt, chờ nghiệm thu."
                    };
                    await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(appointmentTracking);
                    var requestTracking = new RequestTracking
                    {
                        RepairRequestId = repairRequest.RepairRequestId,
                        Status = RequestStatus.AcceptancePendingVerify,
                        UpdatedAt = DateTime.UtcNow.AddHours(7),
                        UpdatedBy = userId,
                        Note = "Báo cáo sửa chữa đã được phê duyệt, chờ nghiệm thu."
                    };
                    await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(requestTracking);
                    var appointmentAssign = repairReport.Appointment.AppointmentAssigns;
                    foreach (var assign in appointmentAssign)
                    {
                        assign.Status = WorkOrderStatus.Completed;
                        assign.ActualEndTime = DateTime.UtcNow.AddHours(7);
                        _unitOfWork.GetRepository<AppointmentAssign>().UpdateAsync(assign);
                    }
                }
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

                default:
                    throw new AppValidationException(
                        $"Role {currentRole} không được phép tạo approval.",
                        StatusCodes.Status403Forbidden);
            }
        }

        /// <summary>
        /// Escalate approval lên cấp cao hơn
        /// </summary>
        private async Task EscalateApprovalAsync(
            ReportApproval currentApproval,
            ApproveReportCreateDto dto,
            AccountRole currentRole)
        {
            var reportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();

            // Đánh dấu approval hiện tại là Approved
            currentApproval.Status = ReportStatus.Approved;
            currentApproval.Comment = dto.Comment ?? "Đã phê duyệt và chuyển lên cấp cao hơn";
            currentApproval.CreatedAt = DateTime.UtcNow.AddHours(7);
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
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await reportApprovalRepo.InsertAsync(newApproval);
        }

        #endregion
    }
}
