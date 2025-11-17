using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ReportDtos
{
    public class ReportFilterDto : PaginateDto
    {
        public DateOnly? Fromdate { get; set; }
        public DateOnly? Todate { get; set; }
        public int? CommonAreaObjectId { get; set; }
        public int? UserId { get; set; }
    }
}
