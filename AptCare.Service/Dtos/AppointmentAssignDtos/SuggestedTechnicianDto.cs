using AptCare.Service.Dtos.AppointmentDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AppointmentAssignDtos
{
    public class SuggestedTechnicianDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime? Birthday { get; set; }
        public double? GapFromPrevious { get; set; }
        public double? GapToNext { get; set; }
        public int AssignCountThatDay { get; set; }
        public int AssignCountThatMonth { get; set; }
        public List<AppointmentDto>? AppointmentsThatDay { get; set; }
    }
}
