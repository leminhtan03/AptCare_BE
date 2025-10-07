using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class FloorCreateDto
    {
        [Required(ErrorMessage = "Mã tòa nhà không được để trống.")]
        [MaxLength(256, ErrorMessage = "Mã tòa nhà không được vượt quá 256 ký tự.")]
        public string BuildingCode { get; set; } = null!;

        [Required(ErrorMessage = "Số tầng không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số tầng phải nằm trong khoảng từ 1 đến 100.")]
        public int FloorNumber { get; set; }

        public string Description { get; set; } = null!;
    }
}
