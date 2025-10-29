using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class InspectionReporService : BaseService<InspectionReporService>, IInspectionReporService
    {
        private readonly IUserContext _userContext;
        public InspectionReporService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InspectionReporService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> GenerateInspectionReportAsync(CreateInspectionReporDto dto)
        {
            try
            {
                var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                var appoRepo = _unitOfWork.GetRepository<Appointment>();
                if (await appoRepo.AnyAsync(predicate: e => e.AppointmentId == dto.AppointmentId && e.Status == AppointmentStatus.Pending))
                    throw new AppValidationException("Cuộc hẹn không tồn tại hoặc đang trong quá trình phân công nhân lực");
                var newInsReport = _mapper.Map<InspectionReport>(dto);
                newInsReport.UserId = _userContext.CurrentUserId;
                await InspecRepo.InsertAsync(newInsReport);
                await _unitOfWork.CommitAsync();

                return "Tạo báo cáo kiểm tra thành công";
            }
            catch (Exception ex)
            {
                throw new Exception("GenerateInspectionReportAsync", ex);
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
                );

                if (inspectionReport == null)
                    throw new AppValidationException("Báo cáo kiểm tra không tồn tại");

                var result = _mapper.Map<InspectionReportDto>(inspectionReport);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("GetInspectionReportByIdAsync", ex);
            }
        }
        public async Task<IPaginate<InspectionReportDto>> GetPaginateInspectionReportsAsync(InspectionReportFilterDto filterDto)
        {
            try
            {
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

                // Get the paginated entity list first
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
                    orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty)
                );

                var mappedItems = _mapper.Map<List<InspectionReportDto>>(paginateEntityResult.Items);

                var paginateResult = new Paginate<InspectionReportDto>
                {
                    Page = paginateEntityResult.Page,
                    Size = paginateEntityResult.Size,
                    Total = paginateEntityResult.Total,
                    Items = mappedItems
                };

                return paginateResult;
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
