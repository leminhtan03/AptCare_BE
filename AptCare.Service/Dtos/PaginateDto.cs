using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos
{
    public class PaginateDto
    {
        public int page { get; set; }
        public int size { get; set; }
        public string? sortBy { get; set; }
        public string? search { get; set; }
        public string? filter { get; set; }
    }
    public class InspectionReportFilterDto : PaginateDto
    {
        public string? FaultType { get; set; }
        public string? SolutionType { get; set; }

        public DateOnly? Fromdate { get; set; }
        public DateOnly? Todate { get; set; }
    }
    public class UserPaginateDto : PaginateDto
    {
        public AccountRole? Role { get; set; }
    }
    public class TransactionFilterDto : PaginateDto
    {
        public int? InvoiceId { get; set; }
        public int? UserId { get; set; }
        public string? Direction { get; set; }
        public string? Status { get; set; }
        public string? Provider { get; set; }   // PayOS/UnKnow
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}