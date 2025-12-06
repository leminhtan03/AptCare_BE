using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InspectionReporDtos
{
    public class InspectionReportDto
    {
        public int InspectionReportId { get; set; }
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public FaultType FaultOwner { get; set; }
        public SolutionType SolutionType { get; set; }
        public string Description { get; set; }
        public string Solution { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AreaName { get; set; }
        public TechnicanDto Technican { get; set; }
        public List<MediaDto>? Medias { get; set; }
        public List<ApprovelReportDto>? ReportApprovals { get; set; }
        public AppointmentDto? Appointment { get; set; }
    }

    public class InspectionReportDetailDto
    {
        public int InspectionReportId { get; set; }
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public FaultType FaultOwner { get; set; }
        public SolutionType SolutionType { get; set; }
        public string Description { get; set; }
        public string Solution { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AreaName { get; set; }
        public TechnicanDto Technican { get; set; }
        public List<MediaDto>? Medias { get; set; }
        public List<ApprovelReportDto>? ReportApprovals { get; set; }
        public AppointmentDto? Appointment { get; set; }
        public ICollection<InvoiceDto>? Invoice { get; set; }
        public List<RepairRequestTaskDto>? RepairRequestTasks { get; set; }
    }
}
