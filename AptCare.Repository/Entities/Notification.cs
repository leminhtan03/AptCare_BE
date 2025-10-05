using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Notification
    {
        [Required]
        public int NotificationId { get; set; }

        public int ReceiverId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        [ForeignKey("ReceiverId")]
        public Account Receiver { get; set; }
    }
}
