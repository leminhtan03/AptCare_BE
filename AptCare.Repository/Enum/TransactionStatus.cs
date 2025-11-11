using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum TransactionStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Cancelled = 3,
        Refunded = 4
    }
}
