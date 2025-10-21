using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.SlotDtos
{
    public class SlotDto
    {
        public int SlotId { get; set; }
        public string SlotName { get; set; } = null!;
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public int DisplayOrder { get; set; }
        public string Status { get; set; } = null!;
    }
}
