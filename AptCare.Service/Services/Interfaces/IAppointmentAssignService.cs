using AptCare.Service.Dtos.AppointmentAssignDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAppointmentAssignService
    {
        Task<string> AssignAppointmentAsync(int appointmentId, int userId);
        Task<string> UpdateAppointmentAssignAsync(int id, AppointmentAssignUpdateDto dto);
        Task<IEnumerable<SuggestedTechnicianDto>> SuggestTechniciansForAppointment(int appointmentId, int? techniqueId);
        Task<string> ConfirmAssignmentAsync(int appointmentId, bool isConfirmed);

    }
}
