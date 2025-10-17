using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Conversation
    {
        [Key]
        public int ConversationId { get; set; }
        [Required]
        public string Title { get; set; } = null!;
        public ICollection<Message>? Messages { get; set; }
        public ICollection<ConversationParticipant>? ConversationParticipants { get; set; }

    }
}
