using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.ChatDtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IMessageService
    {
        Task<string> CreateTextMessageAsync(TextMessageCreateDto dto);
        Task<string> CreateFileMessageAsync(int conversationId, IFormFile file);
        Task<IPaginate<MessageDto>> GetPaginateMessagesAsync(int conversationId, DateTime? before = null, int pageSize = 20);
        Task<MessageDto> GetMessageByIdAsync(int id);
        Task MarkAsDeliveredAsync(int conversationId);
        Task MarkAsReadAsync(int conversationId);
    }
}
