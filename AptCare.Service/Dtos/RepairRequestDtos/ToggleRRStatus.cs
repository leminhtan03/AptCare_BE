using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairRequestDtos
{
    public class ToggleRRStatus
    {
        public int RepairRequestId { get; set; }
        public RequestStatus NewStatus { get; set; }
        public string? Note { get; set; }
    }
}
