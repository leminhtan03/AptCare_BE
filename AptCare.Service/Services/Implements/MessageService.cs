using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Constants;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Hub;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class MessageService : BaseService<MessageService>, IMessageService
    {
        private readonly IUserContext _userContext;
        private readonly INotificationService _notificationService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRabbitMQService _rabbitMQService;

        public MessageService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<MessageService> logger,
            IMapper mapper,
            IUserContext userContext,
            INotificationService notificationService,
            ICloudinaryService cloudinaryService,
            IRabbitMQService rabbitMQService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _notificationService = notificationService;
            _cloudinaryService = cloudinaryService; ;
            _rabbitMQService = rabbitMQService;
        }

        public async Task<MessageDto> CreateTextMessageAsync(TextMessageCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var conversation = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                                    predicate: p => p.ConversationId == dto.ConversationId
                                    );
                if (conversation == null)
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
                await PushMessageNotificationAsync(message, conversation);
                await _unitOfWork.CommitTransactionAsync();

                var result = await _unitOfWork.GetRepository<Message>().ProjectToSingleOrDefaultAsync<MessageDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: m => m.MessageId == message.MessageId,
                    include: i => i.Include(x => x.Sender)
                                   .Include(x => x.ReplyMessage)
                                       .ThenInclude(x => x.Sender)
                                   .Include(x => x.Conversation)
                );

                var image = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    predicate: p => p.Entity == nameof(User) && p.EntityId == userId
                    );
                result.SenderAvatar = image.FilePath;

                return result;
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<MessageDto> CreateFileMessageAsync(int conversationId, IFormFile file)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var conversation = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                                    predicate: p => p.ConversationId == conversationId
                                    );
                if (conversation == null)
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
                    CreatedAt = DateTime.Now,
                    Type = messageType
                };

                await _unitOfWork.GetRepository<Message>().InsertAsync(message);
                await _unitOfWork.CommitAsync();
                await PushMessageNotificationAsync(message, conversation);
                await _unitOfWork.CommitTransactionAsync();

                var result = await _unitOfWork.GetRepository<Message>().ProjectToSingleOrDefaultAsync<MessageDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: m => m.MessageId == message.MessageId,
                    include: i => i.Include(x => x.Sender)
                                   .Include(x => x.ReplyMessage)
                                       .ThenInclude(x => x.Sender)
                                   .Include(x => x.Conversation)
                );

                var image = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    predicate: p => p.Entity == nameof(User) && p.EntityId == userId
                    );
                result.SenderAvatar = image.FilePath;

                return result;
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task PushMessageNotificationAsync(Message message, Conversation conversation)
        {
            var receiverIds = await _unitOfWork.GetRepository<ConversationParticipant>().GetListAsync(
                selector: s => s.ParticipantId,
                predicate: p => p.ConversationId == message.ConversationId && p.ParticipantId != message.SenderId
                );

            var senderName = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: s => string.Concat(s.FirstName, " ", s.LastName),
                predicate: p => p.UserId == message.SenderId
                );

            var descrption = string.Empty;
            var title = string.Empty;

            if (receiverIds.Count > 1)
            {
                title = $"Nhóm: {conversation.Title}";
                descrption = $"{senderName}: ";
            }
            else
            {
                title = senderName;
            }

            var avatar = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                selector: s => s.FilePath,
                predicate: p => p.Entity == nameof(User) && p.EntityId == message.SenderId
                );

            var image = string.IsNullOrEmpty(avatar) ? Constant.AVATAR_DEFAULT_IMAGE : avatar;

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

            //await _notificationService.PushNotificationAsync(new NotificationPushRequestDto
            //{
            //    Title = title,
            //    Description = descrption,
            //    UserIds = receiverIds,
            //    Image = image
            //});
            await _rabbitMQService.PushNotificationAsync(new NotificationPushRequestDto
            {
                Title = title,
                Description = descrption,
                UserIds = receiverIds,
                Image = image
            });
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
                    include: i => i.Include(x => x.Sender)
                                   .Include(x => x.ReplyMessage)
                                       .ThenInclude(x => x.Sender)
                                   .Include(x => x.Conversation),
                    orderBy: q => q.OrderByDescending(m => m.CreatedAt),
                    page: 1,
                    size: pageSize
                );

            foreach (var msg in messages.Items)
            {
                msg.IsMine = msg.SenderId == userId;

                var image = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    predicate: p => p.Entity == nameof(User) && p.EntityId == msg.SenderId
                    );
                msg.SenderAvatar = image.FilePath;
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
                                   .Include(x => x.Conversation)
                );
            if (message == null)
            {
                throw new AppValidationException("Tin nhắn không tồn tại.", StatusCodes.Status404NotFound);
            }

            message.IsMine = message.SenderId == userId;

            var image = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                predicate: p => p.Entity == nameof(User) && p.EntityId == message.SenderId
                );
            message.SenderAvatar = image.FilePath;

            return message;
        }

        public async Task<(IEnumerable<int>, string)> MarkAsDeliveredAsync(int conversationId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;

                var conversationSlug = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                    selector: s => s.Slug,
                    predicate: m => m.ConversationId == conversationId
                );
                if (string.IsNullOrEmpty(conversationSlug))
                {
                    throw new AppValidationException($"Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
                }

                var messages = await _unitOfWork.GetRepository<Message>().GetListAsync(
                    predicate: m => m.ConversationId == conversationId
                                 && m.SenderId != userId
                                 && m.Status == MessageStatus.Sent
                );

                if (!messages.Any())
                    return (null, conversationSlug);

                foreach (var msg in messages)
                {
                    msg.Status = MessageStatus.Delivered;
                }

                _unitOfWork.GetRepository<Message>().UpdateRange(messages);
                await _unitOfWork.CommitAsync();
                return (messages.Select(x => x.MessageId), conversationSlug);
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<(IEnumerable<int>, string)> MarkAsReadAsync(int conversationId)
        {
            try
            {
                var userId = _userContext.CurrentUserId;

                var conversationSlug = await _unitOfWork.GetRepository<Conversation>().SingleOrDefaultAsync(
                   selector: s => s.Slug,
                   predicate: m => m.ConversationId == conversationId
               );
                if (string.IsNullOrEmpty(conversationSlug))
                {
                    throw new AppValidationException($"Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);
                }

                var messages = await _unitOfWork.GetRepository<Message>().GetListAsync(
                    predicate: m => m.ConversationId == conversationId
                                 && m.SenderId != userId
                                 && m.Status != MessageStatus.Read
                    //include: i => i.Include(x => x.Sender)
                    //               .Include(x => x.ReplyMessage)
                    //                   .ThenInclude(x => x.Sender)
                    //               .Include(x => x.Conversation)
                );

                if (!messages.Any())
                    return (null, conversationSlug);

                foreach (var msg in messages)
                {
                    msg.Status = MessageStatus.Read;
                }

                _unitOfWork.GetRepository<Message>().UpdateRange(messages);
                await _unitOfWork.CommitAsync();
                return (messages.Select(x => x.MessageId), conversationSlug);
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

    }
}
