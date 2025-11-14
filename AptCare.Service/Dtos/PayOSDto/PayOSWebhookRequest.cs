using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.PayOSDto
{
    public class PayOSWebhookRequest
    {
        public string code { get; set; } = null!;
        public string desc { get; set; } = null!;
        public PayOSWebhookData data { get; set; } = null!;
        public string signature { get; set; } = null!;
    }
}
