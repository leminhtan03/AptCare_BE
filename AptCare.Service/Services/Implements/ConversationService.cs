using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Repository.Entities;
using AptCare.Service.Dtos.WorkSlotDtos;
using AptCare.Service.Dtos.ChatDtos;
using System.Security.Cryptography.X509Certificates;
using AptCare.Repository.Enum.AccountUserEnum;
using Microsoft.EntityFrameworkCore;
using AptCare.Repository.Enum;

namespace AptCare.Service.Services.Implements
{
    public class ConversationService : BaseService<Conversation>, IConversationService
    {
        private readonly IUserContext _userContext;


        public ConversationService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<Conversation> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateConversationAsync(ConversationCreateDto dto)
        {
            var isExistingConversation = await _unitOfWork.GetRepository<Conversation>().AnyAsync(
                                predicate: p => p.ConversationParticipants.Count == 2
                                                && p.ConversationParticipants.All(cp => dto.UserIds.Contains(cp.ParticipantId))
                                );
            if (isExistingConversation)
            {
                throw new Exception($"Đã tồn tại cuộc trò chuyện của 2 người.");
            }

            var title = "";
            var conversationParticipants = new List<ConversationParticipant>();

            foreach (var userId in dto.UserIds)
            {
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: x => x.UserId == userId
                    );
                if (user == null)
                {
                    throw new Exception($"Người dùng có ID {userId} không tồn tại.");
                }

                title = string.Join(", ", $"{user.FirstName} {user.LastName}");
                conversationParticipants.Add(new ConversationParticipant
                {
                    ParticipantId = userId,
                    JoinedAt = DateTime.UtcNow.AddHours(7),
                    IsMuted = false
                });
            }

            if (!string.IsNullOrEmpty(dto.Title))
            {
                title = dto.Title;
            }

            var conversation = new Conversation
            {
                Title = title,
                ConversationParticipants = conversationParticipants
            };

            await _unitOfWork.GetRepository<Conversation>().InsertAsync(conversation);
            await _unitOfWork.CommitAsync();

            return "Tạo cuộc trò chuyện thành công.";
        }

        public async Task<IEnumerable<ConversationDto>> GetMyConversationsAsync()
        {
            var userId = _userContext.CurrentUserId;
            var conversations = await _unitOfWork.GetRepository<Conversation>().GetListAsync(
                    selector: s => ConvertToDto(s, userId),
                    predicate: x => x.ConversationParticipants.Any(cp => cp.ParticipantId == userId),
                    include: i => i.Include(x => x.ConversationParticipants)
                                        .ThenInclude(x => x.Participant)
                                   .Include(x => x.Messages)
                    );
            return conversations;
        }

        public async Task<ConversationDto> GetConversationByIdAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var conversation = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                    selector: s => ConvertToDto(s, userId),
                    predicate: x => x.ConversationId == id,
                    include: i => i.Include(x => x.ConversationParticipants)
                                        .ThenInclude(x => x.Participant)
                                   .Include(x => x.Messages)
                    );
            if (conversation == null)
            {
                throw new Exception("Cuộc trò chuyện không tồn tại.");
            }
            if (!conversation.Participants.Any(x => x.UserId == userId))
            {
                throw new Exception("Bạn không sở hữu cuộc trò chuyện này.");
            }
            return conversation;
        }

        public async Task<string> MuteConversationAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var conversationParticipant = await _unitOfWork.GetRepository<ConversationParticipant>().SingleOrDefaultAsync(
                    predicate: x => x.ConversationId == id && x.ParticipantId == userId
                    );
            if (conversationParticipant == null)
            {
                throw new Exception("Cuộc trò chuyện không tồn tại.");
            }

            conversationParticipant.IsMuted = true;

            _unitOfWork.GetRepository<ConversationParticipant>().UpdateAsync(conversationParticipant);
            await _unitOfWork.CommitAsync();
            return "Tắt thông báo thành công.";
        }

        public async Task<string> UnmuteConversationAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var conversationParticipant = await _unitOfWork.GetRepository<ConversationParticipant>().SingleOrDefaultAsync(
                    predicate: x => x.ConversationId == id && x.ParticipantId == userId
                    );
            if (conversationParticipant == null)
            {
                throw new Exception("Cuộc trò chuyện không tồn tại.");
            }

            conversationParticipant.IsMuted = false;

            _unitOfWork.GetRepository<ConversationParticipant>().UpdateAsync(conversationParticipant);
            await _unitOfWork.CommitAsync();
            return "Bật thông báo thành công.";
        }



        private ConversationDto ConvertToDto(Conversation conversation, int userId)
        {
            var title = "conversation.Title";
            if (conversation.ConversationParticipants.Count == 2)
            {
                title = conversation.ConversationParticipants.Where(x => x.ParticipantId != userId)
                                                             .Select(x => $"{x.Participant.FirstName} {x.Participant.LastName}")
                                                             .First();
            }

            var isMuted = conversation.ConversationParticipants.Where(x => x.ParticipantId == userId)
                                                               .Select(x => x.IsMuted)
                                                               .First();

            var participants = new List<ParticipantDto>();

            string lastMessage = "";
            var lastMessageEntity = conversation.Messages.OrderByDescending(x => x.CreatedAt).First();
            if (lastMessageEntity != null)
            {
                switch (lastMessageEntity.Type)
                {
                    case MessageType.Text:
                        lastMessage = lastMessageEntity.Content;
                        break;

                    case MessageType.Image:
                        lastMessage = "Đã gửi một hình ảnh";
                        break;

                    case MessageType.File:
                        lastMessage = "Đã gửi một tệp tin";
                        break;

                    case MessageType.Video:
                        lastMessage = "Đã gửi một video";
                        break;

                    case MessageType.Audio:
                        lastMessage = "Đã gửi một tin nhắn thoại";
                        break;

                    case MessageType.System:
                        lastMessage = "[Thông báo hệ thống]";
                        break;

                    case MessageType.Emoji:
                        lastMessage = "Đã gửi một biểu tượng cảm xúc";
                        break;

                    case MessageType.Location:
                        lastMessage = "Đã chia sẻ vị trí";
                        break;

                    default:
                        lastMessage = "Tin nhắn không xác định";
                        break;
                }
            }
            foreach (var cp in conversation.ConversationParticipants)
            {
                participants.Add(new ParticipantDto
                {
                    UserId = cp.Participant.UserId,
                    FirstName = cp.Participant.FirstName,
                    LastName = cp.Participant.LastName,
                    JoinedAt = cp.JoinedAt
                });
            }

            return new ConversationDto
            {
                ConversationId = conversation.ConversationId,
                Title = title,
                IsMuted = isMuted,
                LastMessage = lastMessage,
                Participants = participants
            };
        }
    }
}
