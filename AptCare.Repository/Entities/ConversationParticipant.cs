using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class ConversationParticipant
    {
        [Required]
        [ForeignKey("Conversation")]
        public int ConversationId { get; set; }
        [Required]
        [ForeignKey("Participant")]
        public int ParticipantId { get; set; }
        [Required]
        public DateTime JoinedAt { get; set; }
        [Required]
        public bool IsMuted { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User Participant { get; set; } = null!;
    }
}
