using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.CommonAreaObjectTypeDtos
{
    public class CommonAreaObjectTypeUpdateDto
    {
        [Required(ErrorMessage = "Tên loại đối tượng là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên loại đối tượng không được vượt quá 100 ký tự.")]
        public string TypeName { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }
    }
}
