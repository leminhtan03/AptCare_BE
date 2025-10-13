using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        public string DeviceInfo { get; set; }
    }
}
