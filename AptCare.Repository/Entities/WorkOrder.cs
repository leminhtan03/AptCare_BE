using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class WorkOrder
    {
        [Key]
        public int WorkOrderId { get; set; }

        public int WorkSlotId { get; set; }
        public int TechnicianId { get; set; }
        public DateTime EstimatedStartTime { get; set; }
        public DateTime EstimatedEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public WorkOrderStatus Status { get; set; }

        [ForeignKey(nameof(WorkSlotId))]
        public WorkSlot WorkSlot { get; set; }

        [ForeignKey(nameof(TechnicianId))]
        public User Technician { get; set; }
    }
}
