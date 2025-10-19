using AptCare.Repository.Entities;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using Microsoft.EntityFrameworkCore;
using AptCare.Service.Exceptions;

namespace AptCare.Service.Services.Implements
{
    public class MessageService : BaseService<Message>, IMessageService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;

        public MessageService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<Message> logger,
            IMapper mapper,
            IUserContext userContext,
            ICloudinaryService cloudinaryService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<string> CreateTextMessageAsync(TextMessageCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var isExistingConversation = await _unitOfWork.GetRepository<Conversation>().AnyAsync(
                                    predicate: p => p.ConversationId == dto.ConversationId
                                    );
                if (!isExistingConversation)
                {
                    throw new AppValidationException($"Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (dto.RellyMessageId != null)
                {
                    var isExistingMessage = await _unitOfWork.GetRepository<Message>().AnyAsync(
                                    predicate: p => p.MessageId == dto.RellyMessageId
                                    );
                    if (!isExistingMessage)
                    {
                        throw new AppValidationException($"Tin nhắn không tồn tại.", StatusCodes.Status404NotFound);
                    }
                }

                var message = _mapper.Map<Message>(dto);
                message.SenderId = userId;

                await _unitOfWork.GetRepository<Message>().InsertAsync(message);
                await _unitOfWork.CommitAsync();
                await PushMessageNotificationAsync(message);
                await _unitOfWork.CommitTransactionAsync();

                return "Đã gửi tin nhắn.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> CreateFileMessageAsync(int conversationId, IFormFile file)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var isExistingConversation = await _unitOfWork.GetRepository<Conversation>().AnyAsync(
                                    predicate: p => p.ConversationId == conversationId
                                    );
                if (!isExistingConversation)
                {
                    throw new AppValidationException($"Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (file == null || file.Length == 0)
                    throw new AppValidationException("File không hợp lệ.");

                MessageType messageType;
                if (file.ContentType.StartsWith("image/"))
                    messageType = MessageType.Image;
                else if (file.ContentType.StartsWith("video/"))
                    messageType = MessageType.Video;
                else if (file.ContentType.StartsWith("audio/"))
                    messageType = MessageType.Audio;
                else
                    messageType = MessageType.File;

                var content = await _cloudinaryService.UploadImageAsync(file);
                if (content == null)
                {
                    throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);
                }

                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = userId,
                    Content = content,
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Type = messageType
                };

                await _unitOfWork.GetRepository<Message>().InsertAsync(message);
                await _unitOfWork.CommitAsync();
                await PushMessageNotificationAsync(message);
                await _unitOfWork.CommitTransactionAsync();

                return "Đã gửi tin nhắn.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task PushMessageNotificationAsync(Message message)
        {
            var receiverIds = await _unitOfWork.GetRepository<ConversationParticipant>().GetListAsync(
                selector: s => s.ParticipantId,
                predicate: p => p.ConversationId == message.ConversationId && p.ParticipantId != message.SenderId
                );

            var senderName = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: s => string.Concat(s.FirstName, " ", s.LastName),
                predicate: p => p.UserId == message.SenderId
                );

            var descrption = $"{senderName}: ";

            switch (message.Type)
            {
                case MessageType.Text:
                    descrption = message.Content;
                    break;

                case MessageType.Image:
                    descrption = "Đã gửi một hình ảnh";
                    break;

                case MessageType.File:
                    descrption = "Đã gửi một tệp tin";
                    break;

                case MessageType.Video:
                    descrption = "Đã gửi một video";
                    break;

                case MessageType.Audio:
                    descrption = "Đã gửi một tin nhắn thoại";
                    break;

                case MessageType.System:
                    descrption = "[Thông báo hệ thống]";
                    break;

                case MessageType.Emoji:
                    descrption = "Đã gửi một biểu tượng cảm xúc";
                    break;

                case MessageType.Location:
                    descrption = "Đã chia sẻ vị trí";
                    break;

                default:
                    descrption = "Tin nhắn không xác định";
                    break;
            }

            var notifications = new List<Notification>();

            foreach (var receiverId in receiverIds)
            {
                notifications.Add(new Notification
                {
                    MessageId = message.MessageId,
                    ReceiverId = message.SenderId,
                    Description = descrption,
                    Type = NotificationType.Message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });
            }

            await _unitOfWork.GetRepository<Notification>().InsertRangeAsync(notifications);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IPaginate<MessageDto>> GetPaginateMessagesAsync(int conversationId, DateTime? before, int pageSize)
        {
            var userId = _userContext.CurrentUserId;
            var isExistingConversation = await _unitOfWork.GetRepository<Conversation>().AnyAsync(p => p.ConversationId == conversationId);

            if (!isExistingConversation)
                throw new AppValidationException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);

            var messages = await _unitOfWork.GetRepository<Message>().ProjectToPagingListAsync<MessageDto>(
                    configuration: _mapper.ConfigurationProvider,
                    //parameters: new { CurrentUserId = userId },
                    predicate: m => m.ConversationId == conversationId && (before == null || m.CreatedAt < before),
                    include: i => i.Include(x => x.Sender).Include(x => x.ReplyMessage),
                    orderBy: q => q.OrderByDescending(m => m.CreatedAt),
                    page: 1,
                    size: pageSize
                );

            foreach (var msg in messages.Items)
            {
                msg.IsMine = msg.SenderId == userId;
            }

            return messages;
        }

        public async Task<MessageDto> GetMessageByIdAsync(int id)
        {
            var userId = _userContext.CurrentUserId;
            var message = await _unitOfWork.GetRepository<Message>().ProjectToSingleOrDefaultAsync<MessageDto>(
                    configuration: _mapper.ConfigurationProvider,
                    //parameters: new { CurrentUserId = userId },
                    predicate: m => m.MessageId == id,
                    include: i => i.Include(x => x.Sender)
                                   .Include(x => x.ReplyMessage)
                                       .ThenInclude(x => x.Sender)
                );
            if (message == null)
            {
                throw new AppValidationException("Tin nhắn không tồn tại.", StatusCodes.Status404NotFound);
            }

            message.IsMine = message.SenderId == userId;
            return message;
        }

        public async Task MarkAsDeliveredAsync(int conversationId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var messages = await _unitOfWork.GetRepository<Message>().GetListAsync(
                    predicate: m => m.ConversationId == conversationId
                                 && m.SenderId != userId
                                 && m.Status == MessageStatus.Sent
                );

                if (!messages.Any())
                    return;

                foreach (var msg in messages)
                {
                    msg.Status = MessageStatus.Delivered;
                }

                _unitOfWork.GetRepository<Message>().UpdateRange(messages);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }            
        }

        public async Task MarkAsReadAsync(int conversationId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var messages = await _unitOfWork.GetRepository<Message>().GetListAsync(
                    predicate: m => m.ConversationId == conversationId
                                 && m.SenderId != userId
                                 && m.Status != MessageStatus.Read
                );

                if (!messages.Any())
                    return;

                foreach (var msg in messages)
                {
                    msg.Status = MessageStatus.Read;
                }

                _unitOfWork.GetRepository<Message>().UpdateRange(messages);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

    }
}
