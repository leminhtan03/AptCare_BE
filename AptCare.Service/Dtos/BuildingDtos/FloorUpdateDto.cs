using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class FloorUpdateDto
    {
        [Required(ErrorMessage = "Số tầng không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số tầng phải nằm trong khoảng từ 1 đến 100.")]
        public int FloorNumber { get; set; }

        public string Description { get; set; } = null!;

        public ActiveStatus Status { get; set; }
    }
}
