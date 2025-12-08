using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class MessageServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Message>> _messageRepo = new();
        private readonly Mock<IGenericRepository<Conversation>> _conversationRepo = new();
        private readonly Mock<IGenericRepository<ConversationParticipant>> _participantRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<ICloudinaryService> _cloudinary = new();
        private readonly Mock<ILogger<MessageService>> _logger = new();
        private readonly Mock<IRabbitMQService> _rabbitMQService = new();

        private readonly MessageService _service;

        public MessageServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Message>()).Returns(_messageRepo.Object);
            _uow.Setup(u => u.GetRepository<Conversation>()).Returns(_conversationRepo.Object);
            _uow.Setup(u => u.GetRepository<ConversationParticipant>()).Returns(_participantRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new MessageService(_uow.Object, _logger.Object, _mapper.Object, _userContext.Object, _notification.Object, _cloudinary.Object, _rabbitMQService.Object);
        }

        #region CreateTextMessageAsync Tests

        [Fact]
        public async Task CreateTextMessageAsync_Success_CreatesMessage()
        {
            // Arrange
            var currentUserId = 1;
            var dto = new TextMessageCreateDto
            {
                ConversationId = 1,
                Content = "Hello",
                RellyMessageId = null
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var conversation = new Conversation { ConversationId = 1, Title = "Test" };
            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            var message = new Message { MessageId = 1, ConversationId = 1, SenderId = currentUserId, Content = "Hello" };
            _mapper.Setup(m => m.Map<Message>(dto)).Returns(message);

            var messageDto = new MessageDto { MessageId = 1, Content = "Hello", SenderId = currentUserId };
            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _messageRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<MessageDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(messageDto);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new Media { FilePath = "avatar.jpg" });

            _participantRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<ConversationParticipant, int>>>(),
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync(new List<int> { 2 });

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, string>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync("John Doe");

            // Act
            var result = await _service.CreateTextMessageAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hello", result.Content);
            _messageRepo.Verify(r => r.InsertAsync(It.IsAny<Message>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateTextMessageAsync_Throws_WhenConversationNotExists()
        {
            // Arrange
            var dto = new TextMessageCreateDto { ConversationId = 999, Content = "Hello" };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync((Conversation)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateTextMessageAsync(dto));
            Assert.Equal("Lỗi hệ thống: Cuộc trò chuyện không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateTextMessageAsync_Throws_WhenReplyMessageNotExists()
        {
            // Arrange
            var dto = new TextMessageCreateDto { ConversationId = 1, Content = "Reply", RellyMessageId = 999 };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            var conversation = new Conversation { ConversationId = 1 };
            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            _messageRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateTextMessageAsync(dto));
            Assert.Equal("Lỗi hệ thống: Tin nhắn không tồn tại.", ex.Message);
        }

        #endregion

        #region CreateFileMessageAsync Tests

        [Fact]
        public async Task CreateFileMessageAsync_Success_CreatesImageMessage()
        {
            // Arrange
            var currentUserId = 1;
            var conversationId = 1;

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("image.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var conversation = new Conversation { ConversationId = conversationId };
            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            _cloudinary.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("http://cloudinary.com/image.jpg");

            var messageDto = new MessageDto { MessageId = 1, Type = MessageType.Image.ToString() };
            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _messageRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<MessageDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(messageDto);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new Media { FilePath = "avatar.jpg" });

            _participantRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<ConversationParticipant, int>>>(),
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync(new List<int> { 2 });

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, string>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync("John Doe");

            // Act
            var result = await _service.CreateFileMessageAsync(conversationId, fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(MessageType.Image.ToString(), result.Type);
            _messageRepo.Verify(r => r.InsertAsync(It.IsAny<Message>()), Times.Once);
        }

        [Fact]
        public async Task CreateFileMessageAsync_Throws_WhenFileInvalid()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            var conversation = new Conversation { ConversationId = 1 };
            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateFileMessageAsync(1, fileMock.Object));
            Assert.Equal("Lỗi hệ thống: File không hợp lệ.", ex.Message);
        }

        #endregion

        #region GetPaginateMessagesAsync Tests

        [Fact]
        public async Task GetPaginateMessagesAsync_Success_ReturnsMessages()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(true);

            var messages = new List<MessageDto>
            {
                new MessageDto { MessageId = 1, SenderId = currentUserId, Content = "Hello" },
                new MessageDto { MessageId = 2, SenderId = 2, Content = "Hi" }
            };

            var pagedResult = new Paginate<MessageDto>
            {
                Items = messages,
                Page = 1,
                Size = 20,
                Total = 2,
                TotalPages = 1
            };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _messageRepo.Setup(r => r.ProjectToPagingListAsync<MessageDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IOrderedQueryable<Message>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>(),
                1,
                20
            )).ReturnsAsync(pagedResult);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new Media { FilePath = "avatar.jpg" });

            // Act
            var result = await _service.GetPaginateMessagesAsync(conversationId, null, 20);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.True(result.Items.First().IsMine);
            Assert.False(result.Items.Last().IsMine);
        }

        [Fact]
        public async Task GetPaginateMessagesAsync_Throws_WhenConversationNotExists()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetPaginateMessagesAsync(999, null, 20));
            Assert.Equal("Cuộc trò chuyện không tồn tại.", ex.Message);
        }

        #endregion

        #region GetMessageByIdAsync Tests

        [Fact]
        public async Task GetMessageByIdAsync_Success_ReturnsMessage()
        {
            // Arrange
            var messageId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var messageDto = new MessageDto { MessageId = messageId, SenderId = currentUserId, Content = "Test" };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _messageRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<MessageDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(messageDto);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new Media { FilePath = "avatar.jpg" });

            // Act
            var result = await _service.GetMessageByIdAsync(messageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(messageId, result.MessageId);
            Assert.True(result.IsMine);
        }

        [Fact]
        public async Task GetMessageByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _messageRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<MessageDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync((MessageDto)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetMessageByIdAsync(999));
            Assert.Equal("Tin nhắn không tồn tại.", ex.Message);
        }

        #endregion

        #region MarkAsDeliveredAsync Tests

        [Fact]
        public async Task MarkAsDeliveredAsync_Success_UpdatesMessages()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, string>>>(),
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync("slug-123");

            var messages = new List<Message>
            {
                new Message { MessageId = 1, SenderId = 2, Status = MessageStatus.Sent },
                new Message { MessageId = 2, SenderId = 2, Status = MessageStatus.Sent }
            };

            _messageRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IOrderedQueryable<Message>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(messages);

            // Act
            var result = await _service.MarkAsDeliveredAsync(conversationId);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(2, result.Item1.Count());
            Assert.Equal("slug-123", result.Item2);
            Assert.All(messages, m => Assert.Equal(MessageStatus.Delivered, m.Status));
            _messageRepo.Verify(r => r.UpdateRange(messages), Times.Once);
        }

        [Fact]
        public async Task MarkAsDeliveredAsync_Throws_WhenConversationNotExists()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, string>>>(),
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync((string)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.MarkAsDeliveredAsync(999));
            Assert.Equal("Lỗi hệ thống: Cuộc trò chuyện không tồn tại.", ex.Message);
        }

        #endregion

        #region MarkAsReadAsync Tests

        [Fact]
        public async Task MarkAsReadAsync_Success_UpdatesMessages()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, string>>>(),
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync("slug-123");

            var messages = new List<Message>
            {
                new Message { MessageId = 1, SenderId = 2, Status = MessageStatus.Delivered },
                new Message { MessageId = 2, SenderId = 2, Status = MessageStatus.Sent }
            };

            _messageRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Message, bool>>>(),
                It.IsAny<Func<IQueryable<Message>, IOrderedQueryable<Message>>>(),
                It.IsAny<Func<IQueryable<Message>, IIncludableQueryable<Message, object>>>()
            )).ReturnsAsync(messages);

            // Act
            var result = await _service.MarkAsReadAsync(conversationId);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(2, result.Item1.Count());
            Assert.Equal("slug-123", result.Item2);
            Assert.All(messages, m => Assert.Equal(MessageStatus.Read, m.Status));
            _messageRepo.Verify(r => r.UpdateRange(messages), Times.Once);
        }

        #endregion
    }
}