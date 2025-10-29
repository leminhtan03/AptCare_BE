using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ApproveReportDtos
{
    public class ApproveReportCreateDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = "RepairReport";
        public ReportStatus Status { get; set; }
        public string? Comment { get; set; }
    }
}
