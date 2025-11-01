using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class FCMRequestDto
    {
        [Required]
        public string FcmToken { get; set; } = null!;

        [Required]
        public string DeviceInfo { get; set; } = null!;
    }
}
