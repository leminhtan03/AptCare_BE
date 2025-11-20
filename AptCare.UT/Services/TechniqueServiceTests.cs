using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.SlotDtos;
using AptCare.Service.Dtos.TechniqueDto;
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
    public class TechniqueServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Technique>> _techniqueRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<TechniqueService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly TechniqueService _service;

        public TechniqueServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Technique>()).Returns(_techniqueRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<TechniqueListItemDto>(It.IsAny<string>()))
                .ReturnsAsync((TechniqueListItemDto)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<TechniqueListItemDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<TechniqueListItemDto>)null);

            _service = new TechniqueService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_Success_CreatesTechnique()
        {
            // Arrange
            var dto = new TechniqueCreateDto
            {
                Name = "Plumbing",
                Description = "Plumbing services"
            };

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(false);

            var technique = new Technique { TechniqueId = 1, Name = dto.Name };
            _mapper.Setup(m => m.Map<Technique>(dto)).Returns(technique);
            _mapper.Setup(m => m.Map<TechniqueListItemDto>(technique))
                .Returns(new TechniqueListItemDto { TechniqueId = 1, Name = dto.Name });

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TechniqueId);
            _techniqueRepo.Verify(r => r.InsertAsync(technique), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenTechniqueAlreadyExists()
        {
            // Arrange
            var dto = new TechniqueCreateDto { Name = "Duplicate" };

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.CreateAsync(dto));
            Assert.Equal("An error occurred while creating the technique.", ex.Message);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_Success_UpdatesTechnique()
        {
            // Arrange
            var id = 1;
            var dto = new TechniqueUpdateDto
            {
                Name = "Updated Technique",
                Description = "Updated"
            };

            var technique = new Technique { TechniqueId = id, Name = "Old Name" };

            _techniqueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, System.Linq.IOrderedQueryable<Technique>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(technique);

            _mapper.Setup(m => m.Map(dto, technique));

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            //Assert.NotNull(result);
            _techniqueRepo.Verify(r => r.UpdateAsync(technique), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new TechniqueUpdateDto { Name = "Test" };

            _techniqueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, System.Linq.IOrderedQueryable<Technique>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync((Technique)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateAsync(999, dto));
        }

        #endregion        

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Success_ReturnsTechnique()
        {
            // Arrange
            var id = 1;
            var dto = new TechniqueListItemDto { TechniqueId = id, Name = "Test Technique" };

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(true);

            var technique = new Technique { TechniqueId = id };
            _techniqueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Technique, TechniqueListItemDto>>>(),
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, System.Linq.IOrderedQueryable<Technique>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(dto);

            _mapper.Setup(m => m.Map<TechniqueListItemDto>(technique)).Returns(dto);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.TechniqueId);
        }

        #endregion
    }
}