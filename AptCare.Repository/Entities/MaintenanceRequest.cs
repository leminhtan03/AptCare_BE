using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using AptCare.Repository.Enum;

namespace AptCare.Repository.Entities
{
    public class MaintenanceRequest
    {
        [Key]
        public int MaintenanceRequestId { get; set; }

        public int CommonAreaId { get; set; }
        public string Object { get; set; }
        public string Description { get; set; }
        public int Frequency { get; set; }
        public DateTime NextDay { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }



        [ForeignKey(nameof(CommonAreaId))]
        public CommonArea CommonArea { get; set; }     

        public ICollection<MaintenanceTrackingHistory> MaintenanceTrackingHistories { get; set; }
        public ICollection<RepairRequest> RepairRequests { get; set; }
    }
}
