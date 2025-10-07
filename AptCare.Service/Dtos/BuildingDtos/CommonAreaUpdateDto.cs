using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class CommonAreaUpdateDto
    {
        public int? FloorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AreaCode { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public ActiveStatus Status { get; set; }
    }
}
