using AptCare.Service.Dtos.ChatDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IConversationService
    {
        Task<string> CreateConversationAsync(ConversationCreateDto dto);
        Task<int?> CheckExistingConversationAsync(int userId);
        Task<IEnumerable<ConversationDto>> GetMyConversationsAsync();
        Task<ConversationDto> GetConversationByIdAsync(int id);
        Task<string> MuteConversationAsync(int id);
        Task<string> UnmuteConversationAsync(int id);
    }
}
