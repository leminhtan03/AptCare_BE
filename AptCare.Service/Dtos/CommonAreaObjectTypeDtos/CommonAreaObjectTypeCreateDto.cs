using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.CommonAreaObjectTypeDtos
{
    public class CommonAreaObjectTypeCreateDto
    {
        [Required(ErrorMessage = "Tên loại đối tượng là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên loại đối tượng không được vượt quá 100 ký tự.")]
        public string TypeName { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }
        public List<MaintenanceTaskItemDto>? MaintenanceTasks { get; set; }
    }

    /// <summary>
    /// DTO đơn giản cho MaintenanceTask khi tạo cùng CommonAreaObjectType
    /// </summary>
    public class MaintenanceTaskItemDto
    {
        [Required(ErrorMessage = "Tên nhiệm vụ là bắt buộc.")]
        [MaxLength(256, ErrorMessage = "Tên nhiệm vụ không được vượt quá 256 ký tự.")]
        public string TaskName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Mô tả nhiệm vụ không được vượt quá 1000 ký tự.")]
        public string? TaskDescription { get; set; }

        [MaxLength(500, ErrorMessage = "Công cụ yêu cầu không được vượt quá 500 ký tự.")]
        public string? RequiredTools { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải >= 0.")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessage = "Thời gian dự kiến là bắt buộc.")]
        [Range(0.1, double.MaxValue, ErrorMessage = "Thời gian dự kiến phải > 0.")]
        public double EstimatedDurationMinutes { get; set; }
    }
}
