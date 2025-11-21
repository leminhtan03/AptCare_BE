using AptCare.Repository.Enum;
using AptCare.Service.Dtos.CommonAreaObjectDtos;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ReportDtos
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public List<MediaDto>? Medias { get; set; }
        public UserBasicDto User { get; set; } = null!;
        public CommonAreaObjectDto CommonAreaObject { get; set; } = null!;
    }
}
