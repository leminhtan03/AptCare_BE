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
}
