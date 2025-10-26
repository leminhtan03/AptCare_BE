using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class CreateInforWithAccount
    {
        public string? Password { get; set; }
        public string AccountRole { get; set; }
        public CreateUserDto UserData { get; set; }
    }
}
