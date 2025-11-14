using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.PayOSDto
{
    public class PayOSWebhookData
    {
        public long orderCode { get; set; }
        public long amount { get; set; }
        public string status { get; set; } = null!;
        public string transactionId { get; set; } = null!;
        public long time { get; set; }
    }
}
