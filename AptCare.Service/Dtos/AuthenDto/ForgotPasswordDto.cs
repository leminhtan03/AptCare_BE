using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        public string Email { get; set; }
    }
}
