using AptCare.Repository.Enum.AccountUserEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class CreateAccountForUserDto
    {
        public int UserId { get; set; }
        public string? Role { get; set; } = nameof(AccountRole.Resident);
    }
}
