using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.TransactionDtos
{
    public class PayOSOptions
    {
        public string BaseUrl { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ChecksumKey { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
    }
}

