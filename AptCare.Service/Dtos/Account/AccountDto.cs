using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class AccountDto
    {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
