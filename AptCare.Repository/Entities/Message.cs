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
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        [ForeignKey("Conversation")]
        public int ConversationId { get; set; }
        [ForeignKey("Sender")]
        public int SenderId { get; set; }
        public User Sender { get; set; }
        [ForeignKey("RellyMessage")]
        public int? RellyMessageId { get; set; }
        public string Content { get; set; }
        public string type { get; set; }
        public DateTime CreatedAt { get; set; }
        public MessageStatus Status { get; set; }

        public Message? RellyMessage { get; set; }

        public Conversation Conversation { get; set; }
        public ICollection<Message>? RellyMessages { get; set; }
    }
}
