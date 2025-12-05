using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceScheduleDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class MaintenanceScheduleService : BaseService<MaintenanceScheduleService>, IMaintenanceScheduleService
    {
        private readonly IUserContext _userContext;

        public MaintenanceScheduleService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<MaintenanceScheduleService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateMaintenanceScheduleAsync(MaintenanceScheduleCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.CommonAreaObjectId == dto.CommonAreaObjectId,
                        include: i => i.Include(x => x.MaintenanceSchedule)
                                       .Include(x => x.CommonAreaObjectType.MaintenanceTasks)
                    );

                if (commonAreaObject == null)
                    throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (commonAreaObject.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Đối tượng khu vực chung đã ngưng hoạt động.");

                if (commonAreaObject.MaintenanceSchedule != null)
                    throw new AppValidationException("Đối tượng khu vực chung này đã có lịch bảo trì.", StatusCodes.Status409Conflict);

                if (dto.RequiredTechniqueId.HasValue)
                {
                    var techniqueExists = await _unitOfWork.GetRepository<Technique>()
                        .AnyAsync(predicate: x => x.TechniqueId == dto.RequiredTechniqueId.Value);

                    if (!techniqueExists)
                        throw new AppValidationException("Kỹ thuật yêu cầu không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (dto.FrequencyInDays <= 0)
                    throw new AppValidationException("Chu kỳ bảo trì phải lớn hơn 0 ngày.");

                if (dto.RequiredTechnicians <= 0)
                    throw new AppValidationException("Số lượng kỹ thuật viên yêu cầu phải lớn hơn 0.");

                if (commonAreaObject.CommonAreaObjectType.MaintenanceTasks.Count == 0)
                {
                    throw new AppValidationException("Chưa có công việc bảo trì cho loại đối tượng này.");
                }

                if (dto.NextScheduledDate < DateOnly.FromDateTime(DateTime.Now))
                    throw new AppValidationException("Ngày bảo trì tiếp theo không được trong quá khứ.");

                var schedule = _mapper.Map<MaintenanceSchedule>(dto);
                schedule.CreatedAt = DateTime.Now;
                schedule.Status = ActiveStatus.Active;
                schedule.EstimatedDuration = commonAreaObject.CommonAreaObjectType.MaintenanceTasks.Sum(mt => mt.EstimatedDurationMinutes) / 60;

                await _unitOfWork.GetRepository<MaintenanceSchedule>().InsertAsync(schedule);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return "Tạo lịch bảo trì thành công";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating maintenance schedule");
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> UpdateMaintenanceScheduleAsync(int id, MaintenanceScheduleUpdateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                    .SingleOrDefaultAsync(predicate: x => x.MaintenanceScheduleId == id);

                if (schedule == null)
                    throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

                var userId = _userContext.CurrentUserId;
                var trackingHistories = new List<MaintenanceTrackingHistory>();

                if (dto.Description != null && dto.Description != schedule.Description)
                {
                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.Description),
                        OldValue = schedule.Description,
                        NewValue = dto.Description,
                        UpdatedAt = DateTime.Now
                    });
                    schedule.Description = dto.Description;
                }

                if (dto.FrequencyInDays.HasValue && dto.FrequencyInDays.Value != schedule.FrequencyInDays)
                {
                    if (dto.FrequencyInDays.Value <= 0)
                        throw new AppValidationException("Chu kỳ bảo trì phải lớn hơn 0 ngày.");

                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.FrequencyInDays),
                        OldValue = schedule.FrequencyInDays.ToString(),
                        NewValue = dto.FrequencyInDays.Value.ToString(),
                        UpdatedAt = DateTime.Now
                    });
                    schedule.FrequencyInDays = dto.FrequencyInDays.Value;
                }

                if (dto.NextScheduledDate.HasValue && dto.NextScheduledDate.Value != schedule.NextScheduledDate)
                {
                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.NextScheduledDate),
                        OldValue = schedule.NextScheduledDate.ToString("yyyy-MM-dd"),
                        NewValue = dto.NextScheduledDate.Value.ToString("yyyy-MM-dd"),
                        UpdatedAt = DateTime.Now
                    });
                    schedule.NextScheduledDate = dto.NextScheduledDate.Value;
                }

                if (dto.TimePreference.HasValue && dto.TimePreference.Value != schedule.TimePreference)
                {
                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.TimePreference),
                        OldValue = schedule.TimePreference.ToString(@"hh\:mm\:ss"),
                        NewValue = dto.TimePreference.Value.ToString(@"hh\:mm\:ss"),
                        UpdatedAt = DateTime.Now
                    });
                    schedule.TimePreference = dto.TimePreference.Value;
                }

                if (dto.RequiredTechniqueId.HasValue && dto.RequiredTechniqueId != schedule.RequiredTechniqueId)
                {
                    var techniqueExists = await _unitOfWork.GetRepository<Technique>()
                        .AnyAsync(predicate: x => x.TechniqueId == dto.RequiredTechniqueId.Value);

                    if (!techniqueExists)
                        throw new AppValidationException("Kỹ thuật yêu cầu không tồn tại.", StatusCodes.Status404NotFound);

                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.RequiredTechniqueId),
                        OldValue = schedule.RequiredTechniqueId?.ToString() ?? "null",
                        NewValue = dto.RequiredTechniqueId.Value.ToString(),
                        UpdatedAt = DateTime.Now
                    });
                    schedule.RequiredTechniqueId = dto.RequiredTechniqueId;
                }

                if (dto.RequiredTechnicians.HasValue && dto.RequiredTechnicians.Value != schedule.RequiredTechnicians)
                {
                    if (dto.RequiredTechnicians.Value <= 0)
                        throw new AppValidationException("Số lượng kỹ thuật viên yêu cầu phải lớn hơn 0.");

                    trackingHistories.Add(new MaintenanceTrackingHistory
                    {
                        MaintenanceScheduleId = id,
                        UserId = userId,
                        Field = nameof(schedule.RequiredTechnicians),
                        OldValue = schedule.RequiredTechnicians.ToString(),
                        NewValue = dto.RequiredTechnicians.Value.ToString(),
                        UpdatedAt = DateTime.Now
                    });
                    schedule.RequiredTechnicians = dto.RequiredTechnicians.Value;
                }
                
                // Lưu các thay đổi
                if (trackingHistories.Any())
                {
                    _unitOfWork.GetRepository<MaintenanceSchedule>().UpdateAsync(schedule);

                    foreach (var history in trackingHistories)
                    {
                        await _unitOfWork.GetRepository<MaintenanceTrackingHistory>().InsertAsync(history);
                    }

                    await _unitOfWork.CommitAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                return "Cập nhật lịch bảo trì thành công";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating maintenance schedule {Id}", id);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> DeleteMaintenanceScheduleAsync(int id)
        {
            var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .SingleOrDefaultAsync(predicate: x => x.MaintenanceScheduleId == id);

            if (schedule == null)
                throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            _unitOfWork.GetRepository<MaintenanceSchedule>().DeleteAsync(schedule);
            await _unitOfWork.CommitAsync();

            return "Xóa lịch bảo trì thành công";
        }

        public async Task<string> ActivateMaintenanceScheduleAsync(int id)
        {
            var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .SingleOrDefaultAsync(
                    predicate: x => x.MaintenanceScheduleId == id,
                    include: i => i.Include(x => x.CommonAreaObject)
                );

            if (schedule == null)
                throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            if (schedule.Status == ActiveStatus.Active)
                throw new AppValidationException("Lịch bảo trì đã ở trạng thái hoạt động.");

            if (schedule.CommonAreaObject.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Không thể kích hoạt lịch bảo trì khi đối tượng khu vực chung đã ngưng hoạt động.");

            schedule.Status = ActiveStatus.Active;
            _unitOfWork.GetRepository<MaintenanceSchedule>().UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            return "Kích hoạt lịch bảo trì thành công";
        }

        public async Task<string> InactiveMaintenanceScheduleAsync(int id)
        {
            var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .SingleOrDefaultAsync(predicate: x => x.MaintenanceScheduleId == id);

            if (schedule == null)
                throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            if (schedule.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Lịch bảo trì đã ở trạng thái ngưng hoạt động.");

            schedule.Status = ActiveStatus.Inactive;
            _unitOfWork.GetRepository<MaintenanceSchedule>().UpdateAsync(schedule);
            await _unitOfWork.CommitAsync();

            return "Vô hiệu hóa lịch bảo trì thành công";
        }

        public async Task<MaintenanceScheduleDto> GetMaintenanceScheduleByIdAsync(int id)
        {
            var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .SingleOrDefaultAsync(
                    selector: x => _mapper.Map<MaintenanceScheduleDto>(x),
                    predicate: x => x.MaintenanceScheduleId == id,
                    include: i => i.Include(x => x.CommonAreaObject)
                                       .ThenInclude(x => x.CommonArea)
                                           .ThenInclude(x => x.Floor)
                                   .Include(x => x.RequiredTechnique)
                );

            if (schedule == null)
                throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            return schedule;
        }

        public async Task<IPaginate<MaintenanceScheduleDto>> GetPaginateMaintenanceScheduleAsync(PaginateDto dto, int? commonAreaObjectId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            ActiveStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<ActiveStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            Expression<Func<MaintenanceSchedule, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) ||
                    p.Description.ToLower().Contains(search) ||
                    p.CommonAreaObject.Name.ToLower().Contains(search)) &&
                (filterStatus == null || p.Status == filterStatus) &&
                (commonAreaObjectId == null || p.CommonAreaObjectId == commonAreaObjectId);

            var result = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .GetPagingListAsync(
                    selector: x => _mapper.Map<MaintenanceScheduleDto>(x),
                    predicate: predicate,
                    include: i => i.Include(x => x.CommonAreaObject)
                                       .ThenInclude(x => x.CommonArea)
                                   .Include(x => x.RequiredTechnique),
                    orderBy: BuildOrderBy(dto.sortBy ?? string.Empty),
                    page: page,
                    size: size
                );

            return result;
        }

        public async Task<MaintenanceScheduleDto?> GetByCommonAreaObjectIdAsync(int commonAreaObjectId)
        {
            var schedule = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .SingleOrDefaultAsync(
                    selector: x => _mapper.Map<MaintenanceScheduleDto>(x),
                    predicate: x => x.CommonAreaObjectId == commonAreaObjectId,
                    include: i => i.Include(x => x.CommonAreaObject)
                                       .ThenInclude(x => x.CommonArea)
                                   .Include(x => x.RequiredTechnique)
                );

            return schedule;
        }

        public async Task<IEnumerable<MaintenanceTrackingHistoryDto>> GetTrackingHistoryAsync(int maintenanceScheduleId)
        {
            var scheduleExists = await _unitOfWork.GetRepository<MaintenanceSchedule>()
                .AnyAsync(predicate: x => x.MaintenanceScheduleId == maintenanceScheduleId);

            if (!scheduleExists)
                throw new AppValidationException("Lịch bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            var histories = await _unitOfWork.GetRepository<MaintenanceTrackingHistory>()
                .GetListAsync(
                    selector: x => _mapper.Map<MaintenanceTrackingHistoryDto>(x),
                    predicate: x => x.MaintenanceScheduleId == maintenanceScheduleId,
                    include: i => i.Include(x => x.User),
                    orderBy: o => o.OrderByDescending(x => x.UpdatedAt)
                );

            return histories;
        }

        private Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(p => p.MaintenanceScheduleId);

            return sortBy.ToLower() switch
            {
                "next_date" => q => q.OrderBy(p => p.NextScheduledDate),
                "next_date_desc" => q => q.OrderByDescending(p => p.NextScheduledDate),
                "frequency" => q => q.OrderBy(p => p.FrequencyInDays),
                "frequency_desc" => q => q.OrderByDescending(p => p.FrequencyInDays),
                "common_area_object" => q => q.OrderBy(p => p.CommonAreaObject.Name),
                "common_area_object_desc" => q => q.OrderByDescending(p => p.CommonAreaObject.Name),
                _ => q => q.OrderByDescending(p => p.MaintenanceScheduleId)
            };
        }
    }
}
