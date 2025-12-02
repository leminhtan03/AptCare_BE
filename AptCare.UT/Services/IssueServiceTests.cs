using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.IssueDto;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class IssueServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Issue>> _issueRepo = new();
        private readonly Mock<IGenericRepository<Technique>> _techniqueRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<IssueService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly IssueService _service;

        public IssueServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Issue>()).Returns(_issueRepo.Object);
            _uow.Setup(u => u.GetRepository<Technique>()).Returns(_techniqueRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);


            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<IssueListItemDto>(It.IsAny<string>()))
                .ReturnsAsync((IssueListItemDto)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<IssueListItemDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<IssueListItemDto>)null);


            _service = new IssueService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_Success_CreatesIssue()
        {
            // Arrange
            var dto = new IssueCreateDto
            {
                Name = "Water Leak",
                TechniqueId = 1,
                Description = "Water leak issue"
            };

            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(false);

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(true);

            var issue = new Issue { IssueId = 1, Name = dto.Name };
            _mapper.Setup(m => m.Map<Issue>(dto)).Returns(issue);
            _mapper.Setup(m => m.Map<IssueListItemDto>(issue))
                .Returns(new IssueListItemDto { IssueId = 1, Name = dto.Name });

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.IssueId);
            _issueRepo.Verify(r => r.InsertAsync(issue), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenIssueAlreadyExists()
        {
            // Arrange
            var dto = new IssueCreateDto { Name = "Duplicate", TechniqueId = 1 };

            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.CreateAsync(dto));
            Assert.Equal("Issue đã tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenTechniqueNotExists()
        {
            // Arrange
            var dto = new IssueCreateDto { Name = "Test", TechniqueId = 999 };

            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(false);

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.CreateAsync(dto));
            Assert.Equal("Technique không tồn tại", ex.Message);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_Success_UpdatesIssue()
        {
            // Arrange
            var id = 1;
            var dto = new IssueUpdateDto
            {
                Name = "Updated Issue",
                TechniqueId = 1,
                Description = "Updated"
            };

            var issue = new Issue { IssueId = id, Name = "Old Name" };

            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(issue);

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(true);

            _mapper.Setup(m => m.Map(dto, issue));

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật Issue thành công", result);
            _issueRepo.Verify(r => r.UpdateAsync(issue), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new IssueUpdateDto { Name = "Test" };

            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync((Issue)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateAsync(999, dto));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_Success_SetsStatusInactive()
        {
            // Arrange
            var id = 1;
            var issue = new Issue { IssueId = id, Status = ActiveStatus.Active };

            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(true);

            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(issue);

            // Act
            await _service.DeleteAsync(id);

            // Assert
            Assert.Equal(ActiveStatus.Inactive, issue.Status);
            _issueRepo.Verify(r => r.UpdateAsync(issue), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Throws_WhenNotFound()
        {
            // Arrange
            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.DeleteAsync(999));
            Assert.Equal("Issue không tồn tại", ex.Message);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Success_ReturnsIssue()
        {
            // Arrange
            var id = 1;
            var dto = new IssueListItemDto { IssueId = id, Name = "Test Issue" };

            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(true);

            var issue = new Issue { IssueId = id };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(issue);

            _mapper.Setup(m => m.Map<IssueListItemDto>(issue)).Returns(dto);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.IssueId);
        }

        [Fact]
        public async Task GetByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _issueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.GetByIdAsync(999));
            Assert.Equal("Issue không tồn tại", ex.Message);
        }

        #endregion

        #region ListAsync Tests

        [Fact]
        public async Task ListAsync_Success_ReturnsPaginatedIssues()
        {
            // Arrange
            var dto = new PaginateDto { page = 1, size = 10 };
            var issues = new List<IssueListItemDto>
            {
                new IssueListItemDto { IssueId = 1, Name = "Issue 1" },
                new IssueListItemDto { IssueId = 2, Name = "Issue 2" }
            };

            var pagedResult = new Paginate<IssueListItemDto>
            {
                Items = issues,
                Page = 1,
                Size = 10,
                Total = 2,
                TotalPages = 1
            };

            _issueRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<Issue, IssueListItemDto>>>(),
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            _mapper.Setup(m => m.Map<IssueListItemDto>(It.IsAny<Issue>()))
                .Returns((Issue i) => new IssueListItemDto { IssueId = i.IssueId });

            // Act
            var result = await _service.ListAsync(dto, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
        }

        #endregion

        #region GetActiveIssuesAsync Tests

        [Fact]
        public async Task GetActiveIssuesAsync_ReturnsOnlyActiveIssues()
        {
            // Arrange
            var issues = new List<Issue>
            {
                new Issue { IssueId = 1, Status = ActiveStatus.Active },
                new Issue { IssueId = 2, Status = ActiveStatus.Inactive }
            };
            _issueRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                null, null
            )).ReturnsAsync(issues.Where(i => i.Status == ActiveStatus.Active).ToList());

            // Act
            var result = await _service.GetActiveIssuesAsync();

            // Assert
            Assert.All(result, i => Assert.Equal(ActiveStatus.Active, i.Status));
        }

        #endregion
    }
}