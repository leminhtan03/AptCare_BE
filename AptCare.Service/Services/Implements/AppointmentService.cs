using AptCare.Repository.Entities;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.AppointmentDtos;
using Org.BouncyCastle.Asn1.Ocsp;
using AptCare.Service.Exceptions;
using Microsoft.AspNetCore.Http;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using CloudinaryDotNet;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Dtos.WorkSlotDtos;
using AptCare.Repository.Enum;

namespace AptCare.Service.Services.Implements
{
    public class AppointmentService : BaseService<Appointment>, IAppointmentService
    {
        private readonly IUserContext _userContext;

        public AppointmentService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<Appointment> logger,
            IMapper mapper,
            IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateAppointmentAsync(AppointmentCreateDto dto)
        {
            var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                        predicate: x => x.RepairRequestId == dto.RepairRequestId
                        );
            if (!isExistingRepairRequest)
            {
                throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
            }

            if (dto.StartTime >= dto.EndTime)
            {
                throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            var appointment = _mapper.Map<Appointment>(dto);
            await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Tạo lịch hẹn thành công";
        }

        public async Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                predicate: x => x.AppointmentId == id,
                include: i => i.Include(x => x.AppointmentAssigns)
                );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            if (appointment.StartTime != dto.StartTime || appointment.EndTime != dto.EndTime)
            {
                if (appointment.AppointmentAssigns.Any())
                {
                    throw new AppValidationException("Không thể thay đổi thời gian lịch hẹn khi đã phân công.");
                }
            }

            if (dto.StartTime >= dto.EndTime)
            {
                throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            _mapper.Map(dto, appointment);
            _unitOfWork.GetRepository<Appointment>().UpdateAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Cập nhật lịch hẹn thành công";
        }

        public async Task<string> DeleteAppointmentAsync(int id)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                        predicate: x => x.AppointmentId == id
                        );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            _unitOfWork.GetRepository<Appointment>().DeleteAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Xóa lịch hẹn thành công";
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int id)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => p.AppointmentId == id,
                include: i => i.Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.RepairRequest)
                );

            if (appointment == null)
            {
                throw new AppValidationException("Tầng không tồn tại", StatusCodes.Status404NotFound);
            }

            return appointment;
        }

        public async Task<IPaginate<AppointmentDto>> GetPaginateAppointmentAsync(PaginateDto dto, DateOnly? fromDate, DateOnly? toDate)
        {
            if (fromDate != null && toDate != null && fromDate > toDate)
                throw new AppValidationException("Ngày bắt đầu không thể sau ngày kết thúc");

            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Appointment, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Note.Contains(search)) &&
                (string.IsNullOrEmpty(filter) ||filter.Equals(p.Status.ToString().ToLower())) &&
                (fromDate == null || DateOnly.FromDateTime(p.StartTime) >= fromDate) &&
                (toDate == null || DateOnly.FromDateTime(p.StartTime) <= toDate);

            var result = await _unitOfWork.GetRepository<Appointment>().GetPagingListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.RepairRequest),
                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            return result;
        }

        public async Task<IEnumerable<ResidentAppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate)
        {
            var userId = _userContext.CurrentUserId;
           
            var appointments = await _unitOfWork.GetRepository<Appointment>().GetListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => DateOnly.FromDateTime(p.StartTime) >= fromDate && 
                                DateOnly.FromDateTime(p.StartTime) <= toDate &&
                                p.RepairRequest.Apartment.UserApartments.Any(ua => ua.UserId == userId),                  
                include: i => i.Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.UserApartments)
                                .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                );
           
            var result = appointments.GroupBy(a => DateOnly.FromDateTime(a.StartTime))
                                   .Select(x => new ResidentAppointmentScheduleDto
                                   {
                                       Date = x.Key,
                                       Appointments = x.ToList()
                                   });
            return result;
        }

        public async Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetTechnicianAppointmentScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate)
        {
            var appointments = await _unitOfWork.GetRepository<Appointment>().GetListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => DateOnly.FromDateTime(p.StartTime) >= fromDate &&
                                DateOnly.FromDateTime(p.StartTime) <= toDate &&
                                (technicianId == null || p.AppointmentAssigns.Any(ua => ua.TechnicianId == technicianId)),
                include: i => i.Include(x => x.RepairRequest)
                                .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                );

            var slots = await _unitOfWork.GetRepository<Slot>().GetListAsync(predicate: p => p.Status == ActiveStatus.Active);

            var result = appointments
                            .GroupBy(a => DateOnly.FromDateTime(a.StartTime))
                            .Select(dateGroup => new TechnicianAppointmentScheduleDto
                            {
                                Date = dateGroup.Key,
                                Slots = dateGroup
                                    .GroupBy(x => slots.FirstOrDefault(s => s.FromTime <= x.StartTime.TimeOfDay && 
                                                                            s.ToTime > x.StartTime.TimeOfDay).SlotId)
                                    .Select(slotGroup => new SlotAppointmentDto
                                    {
                                        SlotId = slotGroup.Key,
                                        Appointments = slotGroup
                                            .Select(x => x)
                                            .ToList()
                                    })
                                    .ToList()
                            })
                            .OrderBy(x => x.Date)
                            .ToList();
            return result;
        }

        public async Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetMyTechnicianAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate)
        {
            int technicianId = _userContext.CurrentUserId;
            return await GetTechnicianAppointmentScheduleAsync(technicianId, fromDate, toDate);
        }

        private Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.AppointmentId);

            return sortBy.ToLower() switch
            {
                "start_time" => q => q.OrderBy(p => p.StartTime),
                "fstart_time_desc" => q => q.OrderByDescending(p => p.StartTime),
                _ => q => q.OrderByDescending(p => p.AppointmentId) // Default sort
            };
        }
    } 
}
