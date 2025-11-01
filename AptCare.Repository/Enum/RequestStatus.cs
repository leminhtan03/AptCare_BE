using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum RequestStatus
    {
        Pending = 1,
        Approved = 2, // đã gán kỹ thuật viên
        InProgress = 3,
        Diagnosed = 4,
        CompletedPendingVerify = 5,
        AcceptancePendinhVerify = 6,
        Completed = 7,
        Cancelled = 8
    }
}
