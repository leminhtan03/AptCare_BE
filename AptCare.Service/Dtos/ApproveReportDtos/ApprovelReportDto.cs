using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ApproveReportDtos
{
    public class ApprovelReportDto
    {
        public int ReportApprovalId { get; set; }
        public ReportStatus ReportId { get; set; }
        public int FullName { get; set; }
        public AccountRole Role { get; set; }
        public ReportStatus Status { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
