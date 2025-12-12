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
using AptCare.Service.Exceptions;
using AptCare.Service.Helpers;
using AptCare.Service.Constants;

namespace AptCare.Service.Services.Implements
{
    public class ConversationService : BaseService<ConversationService>, IConversationService
    {
        private readonly IUserContext _userContext;

        public ConversationService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<ConversationService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateConversationAsync(ConversationCreateDto dto)
        {
            var currentUserId = _userContext.CurrentUserId;
            dto.UserIds.Add(currentUserId);

            var isExistingConversation = await _unitOfWork.GetRepository<Conversation>().AnyAsync(
                                predicate: p => p.ConversationParticipants.Count == 2
                                                && p.ConversationParticipants.All(cp => dto.UserIds.Contains(cp.ParticipantId))
                                );
            if (isExistingConversation)
            {
                throw new AppValidationException($"Đã tồn tại cuộc trò chuyện của 2 người.");
            }

            var names = new List<string>();
            var conversationParticipants = new List<ConversationParticipant>();

            foreach (var userId in dto.UserIds)
            {
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: x => x.UserId == userId
                );
                if (user == null)
                {
                    throw new AppValidationException($"Người dùng có ID {userId} không tồn tại.", StatusCodes.Status404NotFound);
                }

                names.Add($"{user.FirstName} {user.LastName}");

                conversationParticipants.Add(new ConversationParticipant
                {
                    ParticipantId = userId,
                    JoinedAt = CurrentDateTime.GetCurrentDateTime,
                    IsMuted = false
                });
            }

            var title = string.Join(", ", names);

            if (!string.IsNullOrEmpty(dto.Title))
            {
                title = dto.Title;
            }

            var conversation = new Conversation
            {
                Title = title,
                Slug = Guid.NewGuid().ToString(),
                ConversationParticipants = conversationParticipants
            };

            await _unitOfWork.GetRepository<Conversation>().InsertAsync(conversation);
            await _unitOfWork.CommitAsync();

            return conversation.ConversationId.ToString();
        }

        public async Task<int?> CheckExistingConversationAsync(int userId)
        {
            var currentUserId = _userContext.CurrentUserId;
            var conversationId = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                selector: s => s.ConversationId,
                predicate: x => x.ConversationParticipants.Count == 2 && 
                                x.ConversationParticipants.All(cp => cp.ParticipantId == currentUserId || cp.ParticipantId == userId),
                include: i => i.Include(x => x.ConversationParticipants)
                );

            return conversationId == 0 ? null : conversationId;
        }

        public async Task<IEnumerable<ConversationDto>> GetMyConversationsAsync()
        {
            var userId = _userContext.CurrentUserId;
            var conversations = await _unitOfWork.GetRepository<Conversation>().GetListAsync(
                    predicate: x => x.ConversationParticipants.Any(cp => cp.ParticipantId == userId),
                    include: i => i.Include(x => x.ConversationParticipants)
                                        .ThenInclude(x => x.Participant)
                                   .Include(x => x.Messages),
                    orderBy: o => o.OrderByDescending(x => x.Messages.OrderByDescending(m => m.CreatedAt).First().CreatedAt)
                    );

            List<ConversationDto> result = new List<ConversationDto>();

            foreach (var conversation in conversations)
            {
                result.Add(await ConvertToConversationDto(conversation, userId));
            }

            return result;
        }

        public async Task<ConversationDto> GetConversationByIdAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var conversation = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                    predicate: x => x.ConversationId == id,
                    include: i => i.Include(x => x.ConversationParticipants)
                                        .ThenInclude(x => x.Participant)
                                   .Include(x => x.Messages)
                    );

            if (conversation == null)
            {
                throw new AppValidationException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (!conversation.ConversationParticipants.Any(x => x.ParticipantId == userId))
            {
                throw new AppValidationException("Bạn không sở hữu cuộc trò chuyện này.");
            }
            return await ConvertToConversationDto(conversation, userId);
        }

        public async Task<string> MuteConversationAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var conversationParticipant = await _unitOfWork.GetRepository<ConversationParticipant>().SingleOrDefaultAsync(
                    predicate: x => x.ConversationId == id && x.ParticipantId == userId
                    );
            if (conversationParticipant == null)
            {
                throw new AppValidationException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
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
                throw new AppValidationException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
            }

            conversationParticipant.IsMuted = false;

            _unitOfWork.GetRepository<ConversationParticipant>().UpdateAsync(conversationParticipant);
            await _unitOfWork.CommitAsync();
            return "Bật thông báo thành công.";
        }

        public async Task<ConversationDto> ConvertToConversationDto(Conversation conversation, int userId)
        {
            var title = conversation.Title;
            string image = "";

            if (conversation.ConversationParticipants.Count == 2)
            {
                var id = conversation.ConversationParticipants.Select(x => x.ParticipantId).FirstOrDefault(x => x != userId);
                                                             
                title = conversation.ConversationParticipants.Where(x => x.ParticipantId == id)
                                                             .Select(x => $"{x.Participant.FirstName} {x.Participant.LastName}")
                                                             .First();
                 
                image = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    selector: s => s.FilePath,
                    predicate: p => p.Entity == nameof(User) && p.EntityId == id && p.Status == ActiveStatus.Active
                    );

            }

            var isMuted = conversation.ConversationParticipants.Where(x => x.ParticipantId == userId)
                                                               .Select(x => x.IsMuted)
                                                               .First();

            var participants = new List<ParticipantDto>();

            string lastMessage = "";

            var lastMessageEntity = conversation.Messages.Any() ? conversation.Messages.OrderByDescending(x => x.CreatedAt).First() : null;
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
                Participants = participants,
                Slug = conversation.Slug,
                Image = string.IsNullOrEmpty(image) ? Constant.AVATAR_DEFAULT_IMAGE : image
            };
        }
    }
}
