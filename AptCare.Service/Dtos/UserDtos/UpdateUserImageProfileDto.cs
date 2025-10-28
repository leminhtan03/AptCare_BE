using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.UserDtos
{
    public class UpdateUserImageProfileDto
    {
        public int UserId { get; set; }
        public IFormFile ImageProfileUrl { get; set; }
    }
}
