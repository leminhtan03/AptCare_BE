using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Service.Dtos.RepairReportDtos
{
    public class CreateRepairReportDto
    {
        [Required(ErrorMessage = "AppointmentId là bắt buộc")]
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Mô tả công việc là bắt buộc")]
        public string WorkDescription { get; set; } = string.Empty;
        public string? Note { get; set; }

        /// <summary>
        /// Danh sách file đính kèm (ảnh trước/sau sửa chữa)
        /// </summary>
        public List<IFormFile>? Files { get; set; }
    }
}
