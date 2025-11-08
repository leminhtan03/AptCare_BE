using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.UserDtos
{
    public class InactivateUserDto
    {
        [Required(ErrorMessage = "Lý do vô hiệu hóa không được để trống")]
        [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }
}