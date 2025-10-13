using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class VerifyEmailDto
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [StringLength(6)]
        public string Otp { get; set; }
    }
}
