using AptCare.Service.Dtos.AppointmentDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<string> CreateAppointmentAsync(AppointmentCreateDto dto);
        Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto);
        Task<string> DeleteAppointmentAsync(int id);
        Task<IEnumerable<AppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate);
    }
}
