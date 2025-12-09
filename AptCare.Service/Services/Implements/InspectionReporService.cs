using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class InspectionReporService : BaseService<InspectionReporService>, IInspectionReporService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRepairRequestService _repairRequestService;
        private readonly IAppointmentService _appointmentService;
        private readonly IReportApprovalService _reportApprovalService;
        private const int TIME_TOLERANCE_SECONDS = 5;

        public InspectionReporService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InspectionReporService> logger, IMapper mapper, IUserContext userContext, ICloudinaryService cloudinaryService, IRepairRequestService repairRequestService, IAppointmentService appointmentService, IReportApprovalService reportApprovalService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _repairRequestService = repairRequestService;
            _appointmentService = appointmentService;
            _reportApprovalService = reportApprovalService;
        }

        public async Task<InspectionReportDto> CreateInspectionReportAsync(CreateInspectionReporDto dto)
        {
            try
            {
                var appoRepo = _unitOfWork.GetRepository<Appointment>();
                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var appointmentExists = await appoRepo.SingleOrDefaultAsync(
                    predicate: e => e.AppointmentId == dto.AppointmentId,
                    include: i => i.Include(o => o.AppointmentTrackings));
                if (appointmentExists == null)
                    throw new AppValidationException("Cuộc hẹn không tồn tại hoặc đang trong quá trình phân công nhân lực");
                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: e => e.Appointments.Any(a => a.AppointmentId == dto.AppointmentId),
                    include: e => e.Include(e => e.Appointments));

                if (repairRequest == null)
                    throw new AppValidationException("Không tìm thấy yêu cầu sửa chữa liên quan");
                var existingReport = await _unitOfWork.GetRepository<InspectionReport>()
                            .SingleOrDefaultAsync(
                                predicate: e => e.AppointmentId == dto.AppointmentId,
                                orderBy: q => q.OrderByDescending(r => r.InspectionReportId),
                                include: i => i.Include(r => r.ReportApprovals)
                            );
                if (existingReport != null && existingReport.ReportApprovals.Any(s => s.Status == ReportStatus.Pending))
                    throw new AppValidationException("Báo cáo kiểm tra cho cuộc hẹn này đang chờ phê duyệt. Vui lòng không tạo báo cáo mới.", StatusCodes.Status400BadRequest);
                if (existingReport != null && existingReport.ReportApprovals.Any(s => s.Status != ReportStatus.Rejected))
                    throw new AppValidationException("Đã tồn tại báo cáo kiểm tra được thông qua vui lòng kiểm tra lại phản hồi.", StatusCodes.Status400BadRequest);
                List<string> uploadedFilePaths = new List<string>();
                if (dto.Files != null && dto.Files.Any())
                {
                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);

                        uploadedFilePaths.Add(filePath);
                    }
                }
                await _unitOfWork.BeginTransactionAsync();
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var newInsReport = _mapper.Map<InspectionReport>(dto);
                newInsReport.UserId = _userContext.CurrentUserId;
                newInsReport.Status = ReportStatus.Pending;
                await InspecRepo.InsertAsync(newInsReport);
                await _unitOfWork.CommitAsync();
                if (existingReport == null)
                {
                    if (!await _appointmentService.ToogleAppoimnentStatus(dto.AppointmentId, "Hoàn thành kiểm tra - chờ duyệt báo cáo kiểm tra", AppointmentStatus.AwaitingIRApproval))
                        throw new AppValidationException("Có lỗi xảy ra khi cập nhật trạng thái cuộc hẹn.", StatusCodes.Status500InternalServerError);
                }
                if (!await _reportApprovalService.CreateApproveReportAsync(new ApproveReportCreateDto
                {
                    ReportId = newInsReport.InspectionReportId,
                    ReportType = "InspectionReport",
                    Status = ReportStatus.Pending
                }))
                {
                    throw new AppValidationException("Lỗi không tạo được approval pending");
                }
                if (uploadedFilePaths.Any())
                {
                    var mediaRepo = _unitOfWork.GetRepository<Media>();
                    var mediaEntities = new List<Media>();

                    for (int i = 0; i < uploadedFilePaths.Count; i++)
                    {
                        mediaEntities.Add(new Media
                        {
                            Entity = nameof(InspectionReport),
                            EntityId = newInsReport.InspectionReportId,
                            FileName = dto.Files[i].FileName,
                            FilePath = uploadedFilePaths[i],
                            ContentType = dto.Files[i].ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                    }

                    await mediaRepo.InsertRangeAsync(mediaEntities);
                    await _unitOfWork.CommitAsync();
                }
                await _unitOfWork.CommitTransactionAsync();
                var result = _mapper.Map<InspectionReportDto>(newInsReport);
                _logger.LogInformation("Created inspection report {ReportId} for appointment {AppointmentId}",
                    newInsReport.InspectionReportId, dto.AppointmentId);

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating inspection report for AppointmentId: {AppointmentId}", dto.AppointmentId);
                throw new AppValidationException(ex.Message);
            }
        }

        public async Task<InspectionReportDto> CreateInspectionMaintenanceReportAsync(InspectionMaintenanceReporCreateDto dto)
        {
            try
            {
                var appoRepo = _unitOfWork.GetRepository<Appointment>();
                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var appointmentExists = await appoRepo.SingleOrDefaultAsync(
                    predicate: e => e.AppointmentId == dto.AppointmentId,
                    include: i => i.Include(o => o.AppointmentTrackings));
                if (appointmentExists == null)
                    throw new AppValidationException("Cuộc hẹn không tồn tại hoặc đang trong quá trình phân công nhân lực");

                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: e => e.Appointments.Any(a => a.AppointmentId == dto.AppointmentId),
                    include: e => e.Include(e => e.Appointments)
                                   .Include(e => e.RepairRequestTasks));

                if (repairRequest == null)
                    throw new AppValidationException("Không tìm thấy yêu cầu sửa chữa liên quan");

                var allRepairRequestTasks = repairRequest.RepairRequestTasks?.ToList() ?? new List<RepairRequestTask>();
                var incompleteTasks = allRepairRequestTasks.Where(t => t.Status == TaskCompletionStatus.Pending).ToList();

                if (incompleteTasks.Any())
                {
                    var incompleteTaskNames = incompleteTasks.Select(t => t.TaskName).ToList();
                    throw new AppValidationException(
                        $"Tất cả nhiệm vụ phải được hoàn thành trước khi tạo báo cáo. Nhiệm vụ chưa hoàn thành: {string.Join(", ", incompleteTaskNames)}",
                        StatusCodes.Status400BadRequest);
                }

                var existingReport = await _unitOfWork.GetRepository<InspectionReport>()
                            .SingleOrDefaultAsync(
                                predicate: e => e.AppointmentId == dto.AppointmentId,
                                orderBy: q => q.OrderByDescending(r => r.InspectionReportId),
                                include: i => i.Include(r => r.ReportApprovals)
                            );
                if (existingReport != null && existingReport.ReportApprovals.Any(s => s.Status == ReportStatus.Pending))
                    throw new AppValidationException("Báo cáo kiểm tra cho cuộc hẹn này đang chờ phê duyệt. Vui lòng không tạo báo cáo mới.", StatusCodes.Status400BadRequest);
                if (existingReport != null && existingReport.ReportApprovals.Any(s => s.Status != ReportStatus.Rejected))
                    throw new AppValidationException("Đã tồn tại báo cáo kiểm tra được thông qua vui lòng kiểm tra lại phản hồi.", StatusCodes.Status400BadRequest);

                List<string> uploadedFilePaths = new List<string>();
                if (dto.Files != null && dto.Files.Any())
                {
                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);

                        uploadedFilePaths.Add(filePath);
                    }
                }

                await _unitOfWork.BeginTransactionAsync();

                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var newInsReport = _mapper.Map<InspectionReport>(dto);
                newInsReport.UserId = _userContext.CurrentUserId;
                newInsReport.Status = ReportStatus.Pending;

                await InspecRepo.InsertAsync(newInsReport);
                await _unitOfWork.CommitAsync();

                if (existingReport == null)
                {
                    if (!await _appointmentService.ToogleAppoimnentStatus(dto.AppointmentId, "Hoàn thành kiểm tra - chờ duyệt báo cáo kiểm tra", AppointmentStatus.AwaitingIRApproval))
                        throw new AppValidationException("Có lỗi xảy ra khi cập nhật trạng thái cuộc hẹn.", StatusCodes.Status500InternalServerError);
                }

                if (!await _reportApprovalService.CreateApproveReportAsync(new ApproveReportCreateDto
                {
                    ReportId = newInsReport.InspectionReportId,
                    ReportType = "InspectionReport",
                    Status = ReportStatus.Pending
                }))
                {
                    throw new AppValidationException("Lỗi không tạo được approval pending");
                }

                if (uploadedFilePaths.Any())
                {
                    var mediaRepo = _unitOfWork.GetRepository<Media>();
                    var mediaEntities = new List<Media>();

                    for (int i = 0; i < uploadedFilePaths.Count; i++)
                    {
                        mediaEntities.Add(new Media
                        {
                            Entity = nameof(InspectionReport),
                            EntityId = newInsReport.InspectionReportId,
                            FileName = dto.Files[i].FileName,
                            FilePath = uploadedFilePaths[i],
                            ContentType = dto.Files[i].ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                    }

                    await mediaRepo.InsertRangeAsync(mediaEntities);
                    await _unitOfWork.CommitAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                var result = _mapper.Map<InspectionReportDto>(newInsReport);
                _logger.LogInformation("Created inspection maintenance report {ReportId} for appointment {AppointmentId}",
                    newInsReport.InspectionReportId, dto.AppointmentId);

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating inspection maintenance report for AppointmentId: {AppointmentId}", dto.AppointmentId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<ICollection<InspectionReportDto>> GetInspectionReportByAppointmentIdAsync(int id)
        {
            try
            {
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var inspectionReport = await InspecRepo.GetListAsync(
                    predicate: e => e.AppointmentId == id,
                    include: q => q.Include(i => i.User)
                                    .ThenInclude(u => u.TechnicianTechniques)
                                        .ThenInclude(tt => tt.Technique)
                                    .Include(i => i.User)
                                        .ThenInclude(u => u.WorkSlots)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.Apartment)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.MaintenanceSchedule)
                                                .ThenInclude(mr => mr.CommonAreaObject)
                                    .Include(ra => ra.ReportApprovals)
                                        .ThenInclude(ra => ra.User)
                                            .ThenInclude(u => u.Account),
                    selector: s => _mapper.Map<InspectionReportDto>(s)

                );

                if (inspectionReport == null)
                    return [];

                foreach (var report in inspectionReport)
                {
                    var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                        selector: s => _mapper.Map<MediaDto>(s),
                        predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == report.InspectionReportId
                        );
                    report.Medias = medias.ToList();
                }
                return inspectionReport;
            }
            catch (Exception ex)
            {
                throw new Exception("GetInspectionReportByIdAsync" + ex.Message);
            }
        }

        public async Task<InspectionReportDetailDto> GetInspectionReportByIdAsync(int id)
        {
            try
            {
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var inspectionReport = await InspecRepo.SingleOrDefaultAsync(
                    predicate: e => e.InspectionReportId == id,
                    include: q => q.Include(i => i.User)
                                    .ThenInclude(u => u.TechnicianTechniques)
                                        .ThenInclude(tt => tt.Technique)
                                    .Include(i => i.User)
                                        .ThenInclude(u => u.WorkSlots)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.Apartment)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.RepairRequestTasks)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.MaintenanceSchedule)
                                                .ThenInclude(mr => mr.CommonAreaObject)
                                    .Include(ra => ra.ReportApprovals)
                                        .ThenInclude(ra => ra.User)
                                            .ThenInclude(u => u.Account)
                );

                if (inspectionReport == null)
                    return new InspectionReportDetailDto();

                var result = _mapper.Map<InspectionReportDetailDto>(inspectionReport);
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: p => p.Entity == nameof(InspectionReport) && p.EntityId == result.InspectionReportId
                    );
                if (medias.Count != 0)
                {
                    result.Medias = medias.ToList();
                }

                var reportCreatedAt = inspectionReport.CreatedAt;
                var minTime = reportCreatedAt.AddSeconds(-TIME_TOLERANCE_SECONDS);
                var maxTime = reportCreatedAt.AddSeconds(TIME_TOLERANCE_SECONDS);

                var invoice = await _unitOfWork.GetRepository<Invoice>().GetListAsync(
                    selector: s => _mapper.Map<InvoiceDto>(s),
                    predicate: x => x.RepairRequestId == inspectionReport.Appointment.RepairRequestId &&
                                    x.CreatedAt >= minTime &&
                                    x.CreatedAt <= maxTime &&
                                    x.CreatedAt < inspectionReport.CreatedAt,
                    include: i => i.Include(x => x.InvoiceAccessories)
                                   .Include(x => x.InvoiceServices),
                    orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                    );
                result.Invoice = invoice;
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("GetInspectionReportByIdAsync", ex);
            }
        }

        public async Task<IPaginate<InspectionReportDetailDto>> GetPaginateInspectionReportsAsync(InspectionReportFilterDto filterDto)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                int page = filterDto.page > 0 ? filterDto.page : 1;
                int size = filterDto.size > 0 ? filterDto.size : 10;
                string search = filterDto.search?.ToLower() ?? string.Empty;
                string filter = filterDto.filter?.ToLower() ?? string.Empty;
                string faultTypeFilter = filterDto.FaultType?.ToLower() ?? string.Empty;
                string solutionTypeFilter = filterDto.SolutionType?.ToLower() ?? string.Empty;

                ReportStatus? filterStatus = null;
                if (!string.IsNullOrEmpty(filter))
                {
                    if (Enum.TryParse<ReportStatus>(filter, true, out var parsedStatus))
                    {
                        filterStatus = parsedStatus;
                    }
                }

                Expression<Func<InspectionReport, bool>> predicate = p =>
                    (string.IsNullOrEmpty(search) ||
                    p.Description.ToLower().Contains(search) ||
                    (p.Appointment != null &&
                     ((p.Appointment.RepairRequest.Apartment != null &&
                     p.Appointment.RepairRequest.Apartment.Room.ToLower().Contains(search)) ||
                     (p.Appointment.RepairRequest.MaintenanceSchedule != null &&
                     p.Appointment.RepairRequest.MaintenanceSchedule.CommonAreaObject != null &&
                     p.Appointment.RepairRequest.MaintenanceSchedule.CommonAreaObject.Name.ToLower().Contains(search))
                     )) ||
                    (!string.IsNullOrEmpty(p.Solution) && p.Solution.ToLower().Contains(search))) &&
                    (p.ReportApprovals != null && p.ReportApprovals.Any(ra => ra.UserId == userId) ||
                     p.Appointment.AppointmentAssigns.Any(s => s.TechnicianId == userId)) &&
                    (string.IsNullOrEmpty(filter) ||
                    p.Status == filterStatus) &&
                    (string.IsNullOrEmpty(faultTypeFilter) ||
                    p.FaultOwner.ToString().ToLower().Contains(faultTypeFilter)) &&
                    (string.IsNullOrEmpty(solutionTypeFilter) ||
                    p.SolutionType.ToString().ToLower().Contains(solutionTypeFilter)) &&
                    (!filterDto.Fromdate.HasValue ||
                    DateOnly.FromDateTime(p.CreatedAt) >= filterDto.Fromdate.Value) &&
                    (!filterDto.Todate.HasValue ||
                    DateOnly.FromDateTime(p.CreatedAt) <= filterDto.Todate.Value);

                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();

                var paginateEntityResult = await InspecRepo.GetPagingListAsync(
                    page: page,
                    size: size,
                    predicate: predicate,
                    include: q => q.Include(i => i.User)
                                    .ThenInclude(u => u.TechnicianTechniques)
                                        .ThenInclude(tt => tt.Technique)
                                    .Include(i => i.User)
                                        .ThenInclude(u => u.WorkSlots)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.Apartment)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.RepairRequestTasks)
                                    .Include(ws => ws.Appointment)
                                        .ThenInclude(a => a.RepairRequest)
                                            .ThenInclude(rr => rr.MaintenanceSchedule)
                                                .ThenInclude(mr => mr.CommonAreaObject)
                                    .Include(ra => ra.ReportApprovals)
                                        .ThenInclude(ra => ra.User)
                                            .ThenInclude(u => u.Account),
                    orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty),
                    selector: s => _mapper.Map<InspectionReportDetailDto>(s)
                );

                foreach (var item in paginateEntityResult.Items)
                {
                    var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                        selector: s => _mapper.Map<MediaDto>(s),
                        predicate: p => p.Entity == nameof(InspectionReport) && p.EntityId == item.InspectionReportId
                    );
                    item.Medias = medias.ToList();

                    var reportCreatedAt = item.CreatedAt;
                    var minTime = reportCreatedAt.AddSeconds(-TIME_TOLERANCE_SECONDS);
                    var maxTime = reportCreatedAt.AddSeconds(TIME_TOLERANCE_SECONDS);

                    var invoice = await _unitOfWork.GetRepository<Invoice>().GetListAsync(
                        selector: s => _mapper.Map<InvoiceDto>(s),
                        predicate: x => x.RepairRequestId == item.Appointment.RepairRequest.RepairRequestId &&
                                        x.CreatedAt >= minTime &&
                                        x.CreatedAt <= maxTime &&
                                        x.CreatedAt < item.CreatedAt,
                        include: i => i.Include(x => x.InvoiceAccessories)
                                       .Include(x => x.InvoiceServices),
                        orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                        );
                    item.Invoice = invoice;
                }
                return paginateEntityResult;
            }
            catch (Exception ex)
            {
                throw new Exception("GetPaginateInspectionReportsAsync", ex);
            }
        }
        public async Task<string> UpdateInspectionReportAsync(int id, UpdateInspectionReporDto dto)
        {
            try
            {
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var existingReportTask = await InspecRepo.SingleOrDefaultAsync(predicate: e => e.InspectionReportId == id);
                _mapper.Map(dto, existingReportTask);
                InspecRepo.UpdateAsync(existingReportTask);
                await _unitOfWork.CommitAsync();
                return "Cập nhật báo cáo kiểm tra thành công";
            }
            catch (Exception ex)
            {
                throw new Exception("UpdateInspectionReportAsync", ex);
            }
        }
        private Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.InspectionReportId);

            return sortBy.ToLower() switch
            {
                "id" => q => q.OrderBy(p => p.InspectionReportId),
                "id_desc" => q => q.OrderByDescending(p => p.InspectionReportId),
                _ => q => q.OrderByDescending(p => p.InspectionReportId)
            };
        }

    }
}
