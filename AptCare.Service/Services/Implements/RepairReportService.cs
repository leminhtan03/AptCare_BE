using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.RepairReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class RepairReportService : BaseService<RepairReportService>, IRepairReportService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IReportApprovalService _reportApprovalService;
        private readonly IAppointmentService _appointmentService;

        public RepairReportService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<RepairReportService> logger, IMapper mapper, IUserContext userContext, ICloudinaryService cloudinaryService, IReportApprovalService reportApprovalService, IAppointmentService appointmentService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _reportApprovalService = reportApprovalService;
            _appointmentService = appointmentService;
        }

        public async Task<RepairReportDto> CreateRepairReportAsync(CreateRepairReportDto dto)
        {
            try
            {
                var repairReportRepo = _unitOfWork.GetRepository<RepairReport>();
                var appointmentRepo = _unitOfWork.GetRepository<Appointment>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                await _unitOfWork.BeginTransactionAsync();

                var appointment = await appointmentRepo.SingleOrDefaultAsync(
                    predicate: a => a.AppointmentId == dto.AppointmentId,
                    include: i => i.Include(a => a.AppointmentTrackings)
                                   .Include(a => a.RepairRequest)
                                   .Include(a => a.RepairReport)
                                   .ThenInclude(a => a.ReportApprovals));
                if (appointment == null)
                {
                    throw new AppValidationException("Cuộc hẹn không tồn tại.", StatusCodes.Status404NotFound);
                }

                var lastTracking = appointment.AppointmentTrackings?.OrderByDescending(at => at.UpdatedAt).FirstOrDefault();
                if (lastTracking?.Status == AppointmentStatus.Pending ||
                    lastTracking?.Status == AppointmentStatus.Assigned)
                {
                    throw new AppValidationException(
                        "Cuộc hẹn chưa bắt đầu hoặc đang chờ phân công.",
                        StatusCodes.Status400BadRequest);
                }
                if (appointment.RepairReport != null)
                {
                    throw new AppValidationException("Cuộc hẹn này đã có báo cáo sửa chữa.", StatusCodes.Status400BadRequest);
                }

                var newReport = _mapper.Map<RepairReport>(dto);
                newReport.UserId = _userContext.CurrentUserId;
                newReport.Status = ReportStatus.Pending;
                newReport.CreatedAt = DateTime.Now;

                await repairReportRepo.InsertAsync(newReport);
                await _unitOfWork.CommitAsync();

                // Upload files nếu có
                if (dto.Files != null && dto.Files.Any())
                {
                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                        {
                            throw new AppValidationException(
                                "Có lỗi xảy ra khi upload file.",
                                StatusCodes.Status500InternalServerError);
                        }

                        await mediaRepo.InsertAsync(new Media
                        {
                            Entity = nameof(RepairReport),
                            EntityId = newReport.RepairReportId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                    }
                    await _unitOfWork.CommitAsync();
                }

                var approvalCreated = await _reportApprovalService.CreateApproveReportAsync(
                    new ApproveReportCreateDto
                    {
                        ReportId = newReport.RepairReportId,
                        ReportType = "RepairReport",
                        Status = ReportStatus.Pending,
                        Comment = dto.Note
                    });

                if (!approvalCreated)
                {
                    throw new AppValidationException("Lỗi không tạo được approval pending.");
                }
                await _unitOfWork.CommitTransactionAsync();

                var result = _mapper.Map<RepairReportDto>(newReport);
                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo RepairReport");
                throw new AppValidationException(ex.Message);
            }
        }

        public async Task<RepairReportDto> GetRepairReportByIdAsync(int id)
        {
            var repairReportRepo = _unitOfWork.GetRepository<RepairReport>();

            var repairReport = await repairReportRepo.SingleOrDefaultAsync(
                predicate: rr => rr.RepairReportId == id,
                include: i => i.Include(rr => rr.User)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.Apartment)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.MaintenanceSchedule)
                                           .ThenInclude(mr => mr.CommonAreaObject)
                               .Include(rr => rr.ReportApprovals)
                                   .ThenInclude(ra => ra.User)
                                       .ThenInclude(u => u.Account));

            if (repairReport == null)
            {
                throw new AppValidationException(
                    "Báo cáo sửa chữa không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            var result = _mapper.Map<RepairReportDto>(repairReport);

            // Lấy media files
            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: m => _mapper.Map<MediaDto>(m),
                predicate: m => m.Entity == nameof(RepairReport) && m.EntityId == result.RepairReportId && m.Status == ActiveStatus.Active);

            result.Medias = medias.ToList();

            return result;
        }

        public async Task<RepairReportDto> GetRepairReportByAppointmentIdAsync(int appointmentId)
        {
            var repairReportRepo = _unitOfWork.GetRepository<RepairReport>();

            var repairReport = await repairReportRepo.SingleOrDefaultAsync(
                predicate: rr => rr.AppointmentId == appointmentId,
                include: i => i.Include(rr => rr.User)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.Apartment)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.MaintenanceSchedule)
                                           .ThenInclude(mr => mr.CommonAreaObject)
                               .Include(rr => rr.ReportApprovals)
                                   .ThenInclude(ra => ra.User)
                                       .ThenInclude(u => u.Account));

            if (repairReport == null)
            {
                throw new AppValidationException(
                    "Không tìm thấy báo cáo sửa chữa cho cuộc hẹn này.",
                    StatusCodes.Status404NotFound);
            }

            var result = _mapper.Map<RepairReportDto>(repairReport);

            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: m => _mapper.Map<MediaDto>(m),
                predicate: m => m.Entity == nameof(RepairReport) && m.EntityId == result.RepairReportId && m.Status == ActiveStatus.Active);

            result.Medias = medias.ToList();

            return result;
        }

        public async Task<IPaginate<RepairReportBasicDto>> GetPaginateRepairReportsAsync(RepairReportFilterDto filterDto)
        {
            var userId = _userContext.CurrentUserId;
            int page = filterDto.page > 0 ? filterDto.page : 1;
            int size = filterDto.size > 0 ? filterDto.size : 10;
            string search = filterDto.search?.ToLower() ?? string.Empty;
            string filter = filterDto.filter?.ToLower() ?? string.Empty;

            ReportStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<ReportStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            Expression<Func<RepairReport, bool>> predicate = rr =>
                (string.IsNullOrEmpty(search) ||
                 rr.Description.ToLower().Contains(search) ||
                 (rr.Appointment != null &&
                  ((rr.Appointment.RepairRequest.Apartment != null &&
                    rr.Appointment.RepairRequest.Apartment.Room.ToLower().Contains(search)) ||
                   (rr.Appointment.RepairRequest.MaintenanceSchedule != null &&
                    rr.Appointment.RepairRequest.MaintenanceSchedule.CommonAreaObject != null &&
                    rr.Appointment.RepairRequest.MaintenanceSchedule.CommonAreaObject.Name.ToLower().Contains(search))))) &&
                    rr.Appointment.RepairReport != null &&
                    (rr.Appointment.RepairReport.ReportApprovals.Any(s => s.UserId == userId) ||
                    (rr.Appointment.RepairReport.Appointment.AppointmentAssigns.Any(s => s.TechnicianId == userId))) &&
                (string.IsNullOrEmpty(filter) ||
                 rr.Status == filterStatus) &&
                (!filterDto.Fromdate.HasValue ||
                 DateOnly.FromDateTime(rr.CreatedAt) >= filterDto.Fromdate.Value) &&
                (!filterDto.Todate.HasValue ||
                 DateOnly.FromDateTime(rr.CreatedAt) <= filterDto.Todate.Value) &&
                (!filterDto.TechnicianId.HasValue ||
                 rr.UserId == filterDto.TechnicianId.Value) &&
                (!filterDto.ApartmentId.HasValue ||
                 (rr.Appointment != null && rr.Appointment.RepairRequest.ApartmentId == filterDto.ApartmentId.Value));

            var repairReportRepo = _unitOfWork.GetRepository<RepairReport>();

            var paginateResult = await repairReportRepo.GetPagingListAsync(
                page: page,
                size: size,
                predicate: predicate,
                include: i => i.Include(rr => rr.User)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.Apartment)
                               .Include(rr => rr.Appointment)
                                   .ThenInclude(a => a.RepairRequest)
                                       .ThenInclude(rr => rr.MaintenanceSchedule)
                                           .ThenInclude(mr => mr.CommonAreaObject),
                orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty),
                selector: rr => _mapper.Map<RepairReportBasicDto>(rr));

            foreach (var item in paginateResult.Items)
            {
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: m => _mapper.Map<MediaDto>(m),
                    predicate: m => m.Entity == nameof(RepairReport) && m.EntityId == item.RepairReportId && m.Status == ActiveStatus.Active);

                item.Medias = medias.ToList();
            }

            return paginateResult;
        }

        public async Task<string> UpdateRepairReportAsync(int id, UpdateRepairReportDto dto)
        {
            var repairReportRepo = _unitOfWork.GetRepository<RepairReport>();

            var repairReport = await repairReportRepo.SingleOrDefaultAsync(
                predicate: rr => rr.RepairReportId == id);

            if (repairReport == null)
            {
                throw new AppValidationException(
                    "Báo cáo sửa chữa không tồn tại.",
                    StatusCodes.Status404NotFound);
            }
            if (repairReport.Status == ReportStatus.Approved)
            {
                throw new AppValidationException(
                    "Không thể cập nhật báo cáo đã được phê duyệt.",
                    StatusCodes.Status400BadRequest);
            }

            _mapper.Map(dto, repairReport);
            repairReportRepo.UpdateAsync(repairReport);
            await _unitOfWork.CommitAsync();

            return "Cập nhật báo cáo sửa chữa thành công.";
        }

        private Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(rr => rr.RepairReportId);

            return sortBy.ToLower() switch
            {
                "id" => q => q.OrderBy(rr => rr.RepairReportId),
                "id_desc" => q => q.OrderByDescending(rr => rr.RepairReportId),
                "date" => q => q.OrderBy(rr => rr.CreatedAt),
                "date_desc" => q => q.OrderByDescending(rr => rr.CreatedAt),
                _ => q => q.OrderByDescending(rr => rr.RepairReportId)
            };
        }
    }
}
