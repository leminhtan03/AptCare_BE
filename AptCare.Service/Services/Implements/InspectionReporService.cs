using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.InspectionReporDtos;
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

                if (!await _appointmentService.ToogleAppoimnentStatus(
                    dto.AppointmentId,
                    "Hoàn thành kiểm tra - chờ duyệt báo cáo kiểm tra",
                    AppointmentStatus.AwaitingIRApproval))
                {
                    throw new AppValidationException("Có lỗi xảy ra khi cập nhật trạng thái cuộc hẹn.", StatusCodes.Status500InternalServerError);
                }
                if (!await _repairRequestService.ToggleRepairRequestStatusAsync(new ToggleRRStatus
                {
                    RepairRequestId = repairRequest.RepairRequestId,
                    Note = "Hoàn thành kiểm tra - chờ duyệt báo cáo.",
                    NewStatus = RequestStatus.Diagnosed
                }))
                {
                    throw new AppValidationException("Có lỗi xảy ra khi cập nhật trạng thái yêu cầu sửa chữa.", StatusCodes.Status500InternalServerError);
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
                            CreatedAt = DateTime.UtcNow.AddHours(7),
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
                throw;
            }
        }

        public async Task<InspectionReportDto> GetInspectionReportByAppointmentIdAsync(int id)
        {
            try
            {
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var inspectionReport = await InspecRepo.SingleOrDefaultAsync(
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
                                            .ThenInclude(rr => rr.MaintenanceRequest)
                                                .ThenInclude(mr => mr.CommonAreaObject)
                                    .Include(ra => ra.ReportApprovals)
                                        .ThenInclude(ra => ra.User)
                                            .ThenInclude(u => u.Account)
                );

                if (inspectionReport == null)
                    return new InspectionReportDto();

                var result = _mapper.Map<InspectionReportDto>(inspectionReport);
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == result.InspectionReportId
                    );
                result.Medias = medias.ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("GetInspectionReportByIdAsync" + ex.Message);
            }
        }

        public async Task<InspectionReportDto> GetInspectionReportByIdAsync(int id)
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
                                            .ThenInclude(rr => rr.MaintenanceRequest)
                                                .ThenInclude(mr => mr.CommonAreaObject)
                                    .Include(ra => ra.ReportApprovals)
                                        .ThenInclude(ra => ra.User)
                                            .ThenInclude(u => u.Account)
                );

                if (inspectionReport == null)
                    return new InspectionReportDto();

                var result = _mapper.Map<InspectionReportDto>(inspectionReport);
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == result.InspectionReportId
                    );
                result.Medias = medias.ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("GetInspectionReportByIdAsync", ex);
            }
        }

        public async Task<IPaginate<InspectionBasicReportDto>> GetPaginateInspectionReportsAsync(InspectionReportFilterDto filterDto)
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

                Expression<Func<InspectionReport, bool>> predicate = p =>
                    (string.IsNullOrEmpty(search) ||
                    p.Description.ToLower().Contains(search) ||
                    (p.Appointment != null &&
                     ((p.Appointment.RepairRequest.Apartment != null &&
                     p.Appointment.RepairRequest.Apartment.Room.ToLower().Contains(search)) ||
                     (p.Appointment.RepairRequest.MaintenanceRequest != null &&
                     p.Appointment.RepairRequest.MaintenanceRequest.CommonAreaObject != null &&
                     p.Appointment.RepairRequest.MaintenanceRequest.CommonAreaObject.Name.ToLower().Contains(search))
                     )) ||
                    (!string.IsNullOrEmpty(p.Solution) && p.Solution.ToLower().Contains(search))) &&
                    (string.IsNullOrEmpty(filter) ||
                    p.Status.ToString().ToLower().Contains(filter)) &&
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
                                            .ThenInclude(rr => rr.MaintenanceRequest)
                                                .ThenInclude(mr => mr.CommonAreaObject),
                    orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty),
                    selector: s => _mapper.Map<InspectionBasicReportDto>(s)
                );

                foreach (var item in paginateEntityResult.Items)
                {
                    var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                        selector: s => _mapper.Map<MediaDto>(s),
                        predicate: p => p.Entity == nameof(InspectionReport) && p.EntityId == item.InspectionReportId
                    );
                    item.Medias = medias.ToList();
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
