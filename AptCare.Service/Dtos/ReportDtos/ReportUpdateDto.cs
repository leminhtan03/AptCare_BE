using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ReportDtos
{
    public class ReportUpdateDto
    {
        [Required(ErrorMessage = "ID đối tượng khu vực chung không được để trống.")]
        public int CommonAreaObjectId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [MaxLength(256, ErrorMessage = "Tiêu đề không được vượt quá 256 ký tự.")]
        public string Title { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public ActiveStatus Status { get; set; }
    }
}
