using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IUserContext
    {
        int CurrentUserId { get; }
        string Role { get; }
        bool IsResident { get; }
        bool IsManager { get; }
        bool IsTechnician { get; }
        bool IsTechnicianLead { get; }
        bool IsReceptionist { get; }
    }

}
