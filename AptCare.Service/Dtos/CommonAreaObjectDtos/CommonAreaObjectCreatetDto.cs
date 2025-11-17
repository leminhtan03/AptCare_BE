using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.CommonAreaObjectDtos
{
    public class CommonAreaObjectCreateDto
    {
        [Required(ErrorMessage = "ID khu vực chung không được để trống.")]
        public int CommonAreaId { get; set; }

        [Required(ErrorMessage = "Tên đối tượng không được để trống.")]
        [MaxLength(256, ErrorMessage = "Tên đối tượng không được vượt quá 256 ký tự.")]
        public string Name { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        public string? Description { get; set; }
    }
}
