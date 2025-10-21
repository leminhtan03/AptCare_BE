using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.SlotDtos
{
    public class SlotCreateDto
    {
        [Required]
        public string SlotName { get; set; } = null!;
        [Required]
        public TimeSpan FromTime { get; set; }
        [Required]
        public TimeSpan ToTime { get; set; }
        [Required]
        public int DisplayOrder { get; set; }
    }
}
