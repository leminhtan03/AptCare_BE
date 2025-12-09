using AptCare.Repository.Enum;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairReportDtos
{
    public class RepairReportBasicDto
    {
        public int RepairReportId { get; set; }

        public int AppointmentId { get; set; }

        public string UserFullName { get; set; } = string.Empty;

        public string WorkDescription { get; set; } = string.Empty;

        public ReportStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ApartmentOrAreaName { get; set; }

        public List<MediaDto>? Medias { get; set; }
        public List<ApprovelReportDto>? ReportApprovals { get; set; }
        public AppointmentDto? Appointment { get; set; }
    }
}
