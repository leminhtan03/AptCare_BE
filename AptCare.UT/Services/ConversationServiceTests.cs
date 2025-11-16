using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AptCare.UT.Services
{
    public class ConversationServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Conversation>> _conversationRepo = new();
        private readonly Mock<IGenericRepository<ConversationParticipant>> _participantRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IGenericRepository<Message>> _messageRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ILogger<ConversationService>> _logger = new();

        private readonly ConversationService _service;

        public ConversationServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Conversation>()).Returns(_conversationRepo.Object);
            _uow.Setup(u => u.GetRepository<ConversationParticipant>()).Returns(_participantRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Message>()).Returns(_messageRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new ConversationService(_uow.Object, _logger.Object, _mapper.Object, _userContext.Object);
        }

        #region CreateConversationAsync Tests

        [Fact]
        public async Task CreateConversationAsync_Success_CreatesNewConversation()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;
            var dto = new ConversationCreateDto
            {
                Title = "Test Conversation",
                UserIds = new List<int> { otherUserId }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            // Check existing conversation - not exists
            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(false);

            // Setup users
            var user1 = new User { UserId = currentUserId, FirstName = "User", LastName = "One" };
            var user2 = new User { UserId = otherUserId, FirstName = "User", LastName = "Two" };

            _userRepo.SetupSequence(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            ))
            .ReturnsAsync(user2)  // First call for otherUserId
            .ReturnsAsync(user1); // Second call for currentUserId

            Conversation insertedConversation = null;
            _conversationRepo.Setup(r => r.InsertAsync(It.IsAny<Conversation>()))
                .Callback<Conversation>(c =>
                {
                    c.ConversationId = 123; // Simulate DB generated ID
                    insertedConversation = c;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateConversationAsync(dto);

            // Assert
            Assert.Equal("123", result);
            Assert.NotNull(insertedConversation);
            Assert.Equal("Test Conversation", insertedConversation.Title);
            Assert.Equal(2, insertedConversation.ConversationParticipants.Count);
            _conversationRepo.Verify(r => r.InsertAsync(It.IsAny<Conversation>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateConversationAsync_Success_AutoGeneratesTitleWhenNotProvided()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;
            var dto = new ConversationCreateDto
            {
                Title = null, // No title provided
                UserIds = new List<int> { otherUserId }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(false);

            var user1 = new User { UserId = currentUserId, FirstName = "John", LastName = "Doe" };
            var user2 = new User { UserId = otherUserId, FirstName = "Jane", LastName = "Smith" };

            _userRepo.SetupSequence(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            ))
            .ReturnsAsync(user2)
            .ReturnsAsync(user1);

            Conversation insertedConversation = null;
            _conversationRepo.Setup(r => r.InsertAsync(It.IsAny<Conversation>()))
                .Callback<Conversation>(c =>
                {
                    c.ConversationId = 456;
                    insertedConversation = c;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateConversationAsync(dto);

            // Assert
            Assert.Equal("456", result);
            Assert.NotNull(insertedConversation);
            Assert.Equal("Jane Smith, John Doe", insertedConversation.Title); // Auto-generated from user names
        }

        [Fact]
        public async Task CreateConversationAsync_Throws_WhenConversationAlreadyExists()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;
            var dto = new ConversationCreateDto
            {
                UserIds = new List<int> { otherUserId }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            // Conversation already exists
            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateConversationAsync(dto));
            Assert.Equal("Đã tồn tại cuộc trò chuyện của 2 người.", ex.Message);
        }

        [Fact]
        public async Task CreateConversationAsync_Throws_WhenUserNotExists()
        {
            // Arrange
            var currentUserId = 1;
            var dto = new ConversationCreateDto
            {
                UserIds = new List<int> { 999 } // Non-existent user
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(false);

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateConversationAsync(dto));
            Assert.Contains("Người dùng có ID 999 không tồn tại", ex.Message);
        }

        #endregion

        #region CheckExistingConversationAsync Tests

        [Fact]
        public async Task CheckExistingConversationAsync_ReturnsConversationId_WhenExists()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;
            var existingConversationId = 100;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, int>>>(),
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(existingConversationId);

            // Act
            var result = await _service.CheckExistingConversationAsync(otherUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingConversationId, result.Value);
        }

        [Fact]
        public async Task CheckExistingConversationAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, int>>>(),
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(0);

            // Act
            var result = await _service.CheckExistingConversationAsync(otherUserId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetMyConversationsAsync Tests

        [Fact]
        public async Task GetMyConversationsAsync_Success_ReturnsConversations()
        {
            // Arrange
            var currentUserId = 1;
            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var conversations = new List<Conversation>
            {
                new Conversation
                {
                    ConversationId = 1,
                    Title = "Conv 1",
                    Slug = "slug-1",
                    ConversationParticipants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant
                        {
                            ParticipantId = currentUserId,
                            JoinedAt = DateTime.UtcNow,
                            IsMuted = false,
                            Participant = new User { UserId = currentUserId, FirstName = "User", LastName = "One" }
                        },
                        new ConversationParticipant
                        {
                            ParticipantId = 2,
                            JoinedAt = DateTime.UtcNow,
                            IsMuted = false,
                            Participant = new User { UserId = 2, FirstName = "User", LastName = "Two" }
                        }
                    },
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            MessageId = 1,
                            Content = "Hello",
                            Type = MessageType.Text,
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                }
            };

            _conversationRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversations);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, string>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((string)null);

            // Act
            var result = await _service.GetMyConversationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var conv = result.First();
            Assert.Equal(1, conv.ConversationId);
            Assert.Equal("Hello", conv.LastMessage);
        }

        #endregion

        #region GetConversationByIdAsync Tests

        [Fact]
        public async Task GetConversationByIdAsync_Success_ReturnsConversation()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var conversation = new Conversation
            {
                ConversationId = conversationId,
                Title = "Test Conv",
                Slug = "slug-1",
                ConversationParticipants = new List<ConversationParticipant>
                {
                    new ConversationParticipant
                    {
                        ParticipantId = currentUserId,
                        JoinedAt = DateTime.UtcNow,
                        IsMuted = false,
                        Participant = new User { UserId = currentUserId, FirstName = "User", LastName = "One" }
                    },
                    new ConversationParticipant
                    {
                        ParticipantId = 2,
                        JoinedAt = DateTime.UtcNow,
                        IsMuted = false,
                        Participant = new User { UserId = 2, FirstName = "User", LastName = "Two" }
                    }
                },
                Messages = new List<Message>()
            };

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, string>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((string)null);

            // Act
            var result = await _service.GetConversationByIdAsync(conversationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(conversationId, result.ConversationId);
            Assert.Equal("User Two", result.Title); // For 2-person chat, title is other user's name
        }

        [Fact]
        public async Task GetConversationByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync((Conversation)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetConversationByIdAsync(999));
            Assert.Equal("Cuộc trò chuyện không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task GetConversationByIdAsync_Throws_WhenUserNotParticipant()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 999; // Not a participant

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var conversation = new Conversation
            {
                ConversationId = conversationId,
                Title = "Test Conv",
                Slug = "slug-1",
                ConversationParticipants = new List<ConversationParticipant>
                {
                    new ConversationParticipant
                    {
                        ParticipantId = 1,
                        JoinedAt = DateTime.UtcNow,
                        Participant = new User { UserId = 1, FirstName = "User", LastName = "One" }
                    }
                },
                Messages = new List<Message>()
            };

            _conversationRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Conversation, bool>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IOrderedQueryable<Conversation>>>(),
                It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()
            )).ReturnsAsync(conversation);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetConversationByIdAsync(conversationId));
            Assert.Equal("Bạn không sở hữu cuộc trò chuyện này.", ex.Message);
        }

        #endregion

        #region MuteConversationAsync Tests

        [Fact]
        public async Task MuteConversationAsync_Success_MutesConversation()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                ParticipantId = currentUserId,
                IsMuted = false
            };

            _participantRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync(participant);

            // Act
            var result = await _service.MuteConversationAsync(conversationId);

            // Assert
            Assert.Equal("Tắt thông báo thành công.", result);
            Assert.True(participant.IsMuted);
            _participantRepo.Verify(r => r.UpdateAsync(participant), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task MuteConversationAsync_Throws_WhenNotFound()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _participantRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync((ConversationParticipant)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.MuteConversationAsync(999));
            Assert.Equal("Cuộc trò chuyện không tồn tại.", ex.Message);
        }

        #endregion

        #region UnmuteConversationAsync Tests

        [Fact]
        public async Task UnmuteConversationAsync_Success_UnmutesConversation()
        {
            // Arrange
            var conversationId = 1;
            var currentUserId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);

            var participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                ParticipantId = currentUserId,
                IsMuted = true
            };

            _participantRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync(participant);

            // Act
            var result = await _service.UnmuteConversationAsync(conversationId);

            // Assert
            Assert.Equal("Bật thông báo thành công.", result);
            Assert.False(participant.IsMuted);
            _participantRepo.Verify(r => r.UpdateAsync(participant), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UnmuteConversationAsync_Throws_WhenNotFound()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _participantRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ConversationParticipant, bool>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IOrderedQueryable<ConversationParticipant>>>(),
                It.IsAny<Func<IQueryable<ConversationParticipant>, IIncludableQueryable<ConversationParticipant, object>>>()
            )).ReturnsAsync((ConversationParticipant)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UnmuteConversationAsync(999));
            Assert.Equal("Cuộc trò chuyện không tồn tại.", ex.Message);
        }

        #endregion
    }
}