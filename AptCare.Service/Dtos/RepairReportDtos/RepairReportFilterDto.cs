using AptCare.Service.Dtos;

namespace AptCare.Service.Dtos.RepairReportDtos
{
    public class RepairReportFilterDto : PaginateDto
    {
        public DateOnly? Fromdate { get; set; }

        public DateOnly? Todate { get; set; }

        public int? TechnicianId { get; set; }

        public int? ApartmentId { get; set; }
    }
}