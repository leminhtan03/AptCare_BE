using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ChatDtos
{
    public class ConversationDto
    {
        public int ConversationId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Image { get; set; } = null!;
        public bool IsMuted { get; set; }
        public string LastMessage { get; set; } = null!;
        public List<ParticipantDto> Participants { get; set; } = null!;
    }

    public class ParticipantDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
    }
}
