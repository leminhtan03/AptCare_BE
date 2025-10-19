using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum AppointmentStatus
    {
        Pending = 1,
        Assigned = 2, 
        Confirmed = 3,      
        InProgress = 4,     
        Completed = 5,      
        Canceled = 6
    }
}
