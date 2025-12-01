using System.Linq.Expressions;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InvoiceDtos;
using PayOS;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

// ALIAS để tránh conflict giữa entity và service implementation
using InvoiceServiceEntity = AptCare.Repository.Entities.InvoiceService;
using InvoiceServiceImpl = AptCare.Service.Services.Implements.InvoiceService;

namespace AptCare.UT.Services
{
    public class InvoiceServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IGenericRepository<InvoiceAccessory>> _invoiceAccessoryRepo = new();
        private readonly Mock<IGenericRepository<Accessory>> _accessoryRepo = new();
        private readonly Mock<IGenericRepository<InvoiceServiceEntity>> _invoiceServiceRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _repairRequestRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ILogger<InvoiceServiceImpl>> _logger = new();

        private readonly InvoiceServiceImpl _service;

        public InvoiceServiceTests()
        {
            // Setup repositories
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<InvoiceAccessory>()).Returns(_invoiceAccessoryRepo.Object);
            _uow.Setup(u => u.GetRepository<Accessory>()).Returns(_accessoryRepo.Object);
            _uow.Setup(u => u.GetRepository<InvoiceServiceEntity>()).Returns(_invoiceServiceRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_repairRequestRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);


            _service = new InvoiceServiceImpl(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object);
        }

        #region CreateInternalInvoiceAsync Tests

        [Fact]
        public async Task CreateInternalInvoiceAsync_Success_CreatesInvoiceWithAccessories()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 1, Quantity = 2 }
                },
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "Labor", Price = 500 }
                }
            };

            var repairRequest = new RepairRequest { RepairRequestId = 1 };
            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Mock accessory
            var accessory = new Accessory
            {
                AccessoryId = 1,
                Name = "Test Accessory",
                Price = 100,
                Quantity = 10,
                Status = ActiveStatus.Active
            };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, System.Linq.IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(accessory);

            // Mock invoice mapping
            var invoice = new Invoice
            {
                InvoiceId = 1,
                RepairRequestId = dto.RepairRequestId,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>(),
                Type = InvoiceType.InternalRepair,
                Status = InvoiceStatus.Draft,
                IsChargeable = true,
                TotalAmount = 0
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            // Mock insert operations
            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Callback<Invoice>(inv => inv.InvoiceId = 1)
                .Returns(Task.CompletedTask);

            _invoiceAccessoryRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceAccessory>>()))
                .Returns(Task.CompletedTask);

            _invoiceServiceRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceServiceEntity>>()))
                .Returns(Task.CompletedTask);

            _accessoryRepo.Setup(r => r.UpdateAsync(It.IsAny<Accessory>()))
                .Verifiable();

            // Act
            var result = await _service.CreateInternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai sửa chữa thành công.", result);
            Assert.Equal(8, accessory.Quantity); // 10 - 2 = 8
            _invoiceRepo.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Once);
            _accessoryRepo.Verify(r => r.UpdateAsync(It.IsAny<Accessory>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenRepairRequestNotExists()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 999,
                Accessories = new List<InvoiceAccessoryInternalCreateDto>(),
                Services = new List<ServiceCreateDto>()
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateInternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Yêu cầu sửa chữa không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenAccessoryNotFound()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 999, Quantity = 1 }
                },
                Services = new List<ServiceCreateDto>()
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var invoice = new Invoice
            {
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, System.Linq.IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((Accessory)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateInternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Phụ kiện không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenInsufficientAccessoryQuantity()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 1, Quantity = 20 }
                },
                Services = new List<ServiceCreateDto>()
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var accessory = new Accessory
            {
                AccessoryId = 1,
                Name = "Test Accessory",
                Quantity = 10,
                Status = ActiveStatus.Active
            };

            var invoice = new Invoice
            {
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, System.Linq.IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(accessory);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateInternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Phụ kiện trong kho không đủ số lượng.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Success_WithoutAccessories()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryInternalCreateDto>(),
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "Labor Only", Price = 500 }
                }
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var invoice = new Invoice
            {
                InvoiceId = 1,
                RepairRequestId = dto.RepairRequestId,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>(),
                Type = InvoiceType.InternalRepair,
                Status = InvoiceStatus.Draft
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _invoiceServiceRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceServiceEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateInternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai sửa chữa thành công.", result);
            _invoiceRepo.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Once);
            _accessoryRepo.Verify(r => r.UpdateAsync(It.IsAny<Accessory>()), Times.Never);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        #endregion

        #region CreateExternalInvoiceAsync Tests

        [Fact]
        public async Task CreateExternalInvoiceAsync_Success_CreatesExternalInvoice()
        {
            // Arrange
            var dto = new InvoiceExternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryExternalCreateDto>
                {
                    new InvoiceAccessoryExternalCreateDto
                    {
                        Name = "External Accessory",
                        Quantity = 2,
                        Price = 150
                    }
                },
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "External Labor", Price = 800 }
                }
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var invoice = new Invoice
            {
                InvoiceId = 1,
                RepairRequestId = dto.RepairRequestId,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>(),
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Draft
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _invoiceAccessoryRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceAccessory>>()))
                .Returns(Task.CompletedTask);

            _invoiceServiceRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceServiceEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateExternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai bên thứ 3 thành công.", result);
            _invoiceRepo.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExternalInvoiceAsync_Throws_WhenRepairRequestNotExists()
        {
            // Arrange
            var dto = new InvoiceExternalCreateDto
            {
                RepairRequestId = 999,
                Accessories = new List<InvoiceAccessoryExternalCreateDto>(),
                Services = new List<ServiceCreateDto>()
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateExternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Yêu cầu sửa chữa không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExternalInvoiceAsync_Success_WithoutAccessories()
        {
            // Arrange
            var dto = new InvoiceExternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryExternalCreateDto>(),
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "External Service", Price = 1000 }
                }
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var invoice = new Invoice
            {
                InvoiceId = 1,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>(),
                Type = InvoiceType.ExternalContractor
            };
            _mapper.Setup(m => m.Map<Invoice>(dto)).Returns(invoice);

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _invoiceServiceRepo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<InvoiceServiceEntity>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateExternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai bên thứ 3 thành công.", result);
            _invoiceRepo.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Once);
        }

        #endregion

        #region GetInvoicesAsync Tests

        [Fact]
        public async Task GetInvoicesAsync_Success_ReturnsInvoices()
        {
            // Arrange
            var repairRequestId = 1;
            var invoices = new List<InvoiceDto>
            {
                new InvoiceDto { InvoiceId = 1, RepairRequestId = repairRequestId },
                new InvoiceDto { InvoiceId = 2, RepairRequestId = repairRequestId }
            };

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Invoice, InvoiceDto>();
            });
            _mapper.Setup(m => m.ConfigurationProvider).Returns(mapperConfig);

            _invoiceRepo.Setup(r => r.ProjectToListAsync<InvoiceDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoices);

            // Act
            var result = await _service.GetInvoicesAsync(repairRequestId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, invoice => Assert.Equal(repairRequestId, invoice.RepairRequestId));
        }

        [Fact]
        public async Task GetInvoicesAsync_Throws_WhenRepairRequestNotExists()
        {
            // Arrange
            var repairRequestId = 999;

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetInvoicesAsync(repairRequestId));
            Assert.Equal("Yêu cầu sửa chữa không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task GetInvoicesAsync_Success_ReturnsEmptyList()
        {
            // Arrange
            var repairRequestId = 1;

            _repairRequestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Invoice, InvoiceDto>();
            });
            _mapper.Setup(m => m.ConfigurationProvider).Returns(mapperConfig);

            _invoiceRepo.Setup(r => r.ProjectToListAsync<InvoiceDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(new List<InvoiceDto>());

            // Act
            var result = await _service.GetInvoicesAsync(repairRequestId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}