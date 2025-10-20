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
                throw new AppValidationException("Yêu cầu sửa chửa không tồn tại.", StatusCodes.Status404NotFound);
            }

            var appointment = _mapper.Map<Appointment>(dto);
            await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Tạo lịch hẹn thành công";
        }

        public async Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                        predicate: x => x.AppointmentId == id
                        );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
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

        public async Task<IEnumerable<AppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate)
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
                                   .Select(x => new AppointmentScheduleDto
                                   {
                                       Date = x.Key,
                                       Appointments = x.ToList()
                                   });
            return result;
        }
    } 
}
