using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairReportDtos
{
    public class RepairReportDto
    {
        public int RepairReportId { get; set; }

        public int AppointmentId { get; set; }

        public int UserId { get; set; }

        public string UserFullName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? Note { get; set; }

        public ReportStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public AppointmentDto? Appointment { get; set; }

        public List<MediaDto>? Medias { get; set; }

        public List<ApprovelReportDto>? ReportApprovals { get; set; }
    }
}