using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class ResetPasswordResponseDto
    {
        public string Message { get; set; }
        public int AccountId { get; set; }
    }
}
