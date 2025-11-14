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
        AwaitingPayment = 2,
        PartiallyPaid = 3,
        PaidToContractor = 4,
        Paid = 5,
        Cancelled = 6
    }
}
