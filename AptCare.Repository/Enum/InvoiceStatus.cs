using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum InvoiceStatus
    {
        Draft = 1,
        Approved = 2,
        AwaitingPayment = 3,
        Paid = 4,
        Cancelled = 5,
        Rejected = 6
    }
}
