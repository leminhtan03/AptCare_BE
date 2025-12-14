using System.Linq.Expressions;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ILogger<InvoiceServiceImpl>> _logger = new();
        private readonly Mock<IS3FileService> _s3FileService = new();
        private readonly Mock<ICloudinaryService> _cloudinaryService = new();

        private readonly InvoiceServiceImpl _service;

        public InvoiceServiceTests()
        {
            // Setup repositories
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<InvoiceAccessory>()).Returns(_invoiceAccessoryRepo.Object);
            _uow.Setup(u => u.GetRepository<Accessory>()).Returns(_accessoryRepo.Object);
            _uow.Setup(u => u.GetRepository<InvoiceServiceEntity>()).Returns(_invoiceServiceRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_repairRequestRepo.Object);
            _uow.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new InvoiceServiceImpl(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _s3FileService.Object,
                _cloudinaryService.Object);
        }

        #region CreateInternalInvoiceAsync Tests

        [Fact]
        public async Task CreateInternalInvoiceAsync_Success_CreatesInvoiceWithAvailableAccessories()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 1, Quantity = 2 }
                },
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "Labor", Price = 500 }
                }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.InProgress, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

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

            _invoiceRepo.Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateInternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai sửa chữa thành công.", result);
            _invoiceRepo.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Success_WithAccessoriesToPurchase()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 1, Quantity = 2 }
                },
                AccessoriesToPurchase = new List<InvoiceAccessoryPurchaseCreateDto>
                {
                    new InvoiceAccessoryPurchaseCreateDto
                    {
                        AccessoryId = 2,
                        Name = "New Accessory",
                        Quantity = 5,
                        PurchasePrice = 200
                    }
                },
                Services = new List<ServiceCreateDto>
                {
                    new ServiceCreateDto { Name = "Labor", Price = 500 }
                }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.InProgress, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            var accessories = new Dictionary<int, Accessory>
            {
                { 1, new Accessory
                    {
                        AccessoryId = 1,
                        Name = "Available Accessory",
                        Price = 100,
                        Quantity = 10,
                        Status = ActiveStatus.Active
                    }
                },
                { 2, new Accessory
                    {
                        AccessoryId = 2,
                        Name = "Purchase Accessory",
                        Price = 200,
                        Quantity = 0,
                        Status = ActiveStatus.Active
                    }
                }
            };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, System.Linq.IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((
                Expression<Func<Accessory, bool>> predicate,
                Func<System.Linq.IQueryable<Accessory>, System.Linq.IOrderedQueryable<Accessory>> orderBy,
                Func<System.Linq.IQueryable<Accessory>, IIncludableQueryable<Accessory, object>> include) =>
            {
                // ✅ Compile expression và test với từng accessory
                var func = predicate.Compile();
                return accessories.Values.FirstOrDefault(a => func(a));
            });
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

            // Act
            var result = await _service.CreateInternalInvoiceAsync(dto);

            // Assert
            Assert.Equal("Tạo biên lai sửa chữa thành công.", result);

            // ✅ FIXED - Chỉ tạo 1 invoice duy nhất
            _invoiceRepo.Verify(r => r.InsertAsync(It.Is<Invoice>(i =>
                i.Type == InvoiceType.InternalRepair &&
                i.InvoiceAccessories.Any(a => a.SourceType == InvoiceAccessorySourceType.FromStock) &&
                i.InvoiceAccessories.Any(a => a.SourceType == InvoiceAccessorySourceType.ToBePurchased)
            )), Times.Once);

            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenRepairRequestNotExists()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 999,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>(),
                Services = new List<ServiceCreateDto>()
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateInternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Yêu cầu sửa chữa không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenRepairRequestNotInProgress()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>(),
                Services = new List<ServiceCreateDto>()
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.Pending, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateInternalInvoiceAsync(dto));
            Assert.Contains("Trạng thái sửa chữa đang là", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenAccessoryNotFound()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 999, Quantity = 1 }
                },
                Services = new List<ServiceCreateDto>()
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.InProgress, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

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
            Assert.Contains("không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInternalInvoiceAsync_Throws_WhenInsufficientAccessoryQuantity()
        {
            // Arrange
            var dto = new InvoiceInternalCreateDto
            {
                RepairRequestId = 1,
                AvailableAccessories = new List<InvoiceAccessoryInternalCreateDto>
                {
                    new InvoiceAccessoryInternalCreateDto { AccessoryId = 1, Quantity = 20 }
                },
                Services = new List<ServiceCreateDto>()
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.InProgress, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

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
            Assert.Contains("không đủ số lượng", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
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

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.InProgress, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

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

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateExternalInvoiceAsync(dto));
            Assert.Equal("Lỗi hệ thống: Yêu cầu sửa chữa không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExternalInvoiceAsync_Throws_WhenRepairRequestNotInProgress()
        {
            // Arrange
            var dto = new InvoiceExternalCreateDto
            {
                RepairRequestId = 1,
                Accessories = new List<InvoiceAccessoryExternalCreateDto>(),
                Services = new List<ServiceCreateDto>()
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.Completed, UpdatedAt = DateTime.Now }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateExternalInvoiceAsync(dto));
            Assert.Contains("Trạng thái sửa chữa đang là", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        #endregion

        #region GetInvoicesAsync Tests

        [Fact]
        public async Task GetInvoicesAsync_Success_ReturnsInvoicesExcludingDraftAndCancelled()
        {
            // Arrange
            var repairRequestId = 1;
            var invoices = new List<InvoiceDto>
            {
                new InvoiceDto { InvoiceId = 1, RepairRequestId = repairRequestId, Status = InvoiceStatus.Approved.ToString() },
                new InvoiceDto { InvoiceId = 2, RepairRequestId = repairRequestId, Status = InvoiceStatus.Paid.ToString() }
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
            Assert.All(result, invoice => Assert.NotEqual(InvoiceStatus.Draft.ToString(), invoice.Status));
            Assert.All(result, invoice => Assert.NotEqual(InvoiceStatus.Cancelled.ToString(), invoice.Status));
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

        #region ConfirmExternalContractorPaymentAsync Tests

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Success_WithoutReceipt()
        {
            // Arrange
            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 1,
                Note = "Đã thanh toán qua chuyển khoản"
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Approved,
                IsChargeable = false,
                TotalAmount = 1000000,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };

            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = 1,
                Status = TransactionStatus.Pending,
                Direction = TransactionDirection.Expense,
                Amount = 1000000,
                Description = "Thanh toán nhà thầu"
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, System.Linq.IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transaction);

            _transactionRepo.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Verifiable();
            _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Verifiable();

            // Act
            var result = await _service.ConfirmExternalContractorPaymentAsync(dto);

            // Assert
            Assert.Contains("Xác nhận đã thanh toán cho nhà thầu thành công", result);
            Assert.Equal(TransactionStatus.Success, transaction.Status);
            Assert.Equal(InvoiceStatus.Paid, invoice.Status);
            Assert.NotNull(transaction.PaidAt);
            _transactionRepo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Once);
            _invoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Success_WithImageReceipt()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("receipt.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 1,
                Note = "Đã thanh toán tiền mặt",
                PaymentReceipt = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Approved,
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };

            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = 1,
                Status = TransactionStatus.Pending,
                Direction = TransactionDirection.Expense
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, System.Linq.IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transaction);

            _cloudinaryService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("cloudinary_receipt_url");

            _mediaRepo.Setup(r => r.InsertAsync(It.IsAny<Media>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ConfirmExternalContractorPaymentAsync(dto);

            // Assert
            Assert.Contains("Xác nhận đã thanh toán cho nhà thầu thành công", result);
            _cloudinaryService.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Success_WithPdfReceipt()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("receipt.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(2048);

            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 1,
                Note = "Đã thanh toán",
                PaymentReceipt = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Approved,
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };

            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = 1,
                Status = TransactionStatus.Pending,
                Direction = TransactionDirection.Expense
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, System.Linq.IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transaction);

            _s3FileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("s3_receipt_key");

            _mediaRepo.Setup(r => r.InsertAsync(It.IsAny<Media>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ConfirmExternalContractorPaymentAsync(dto);

            // Assert
            Assert.Contains("Xác nhận đã thanh toán cho nhà thầu thành công", result);
            _s3FileService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Throws_WhenInvoiceNotFound()
        {
            // Arrange
            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 999,
                Note = "Test"
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync((Invoice)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ConfirmExternalContractorPaymentAsync(dto));
            Assert.Contains("Không tìm thấy invoice", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Throws_WhenInvoiceIsChargeable()
        {
            // Arrange
            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 1,
                Note = "Test"
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Approved,
                IsChargeable = true,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ConfirmExternalContractorPaymentAsync(dto));
            Assert.Contains("do cư dân trả", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmExternalContractorPaymentAsync_Throws_WhenTransactionNotFound()
        {
            // Arrange
            var dto = new ExternalContractorPaymentConfirmDto
            {
                InvoiceId = 1,
                Note = "Test"
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                Status = InvoiceStatus.Approved,
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>(),
                InvoiceServices = new List<InvoiceServiceEntity>()
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, System.Linq.IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, System.Linq.IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync((Transaction)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ConfirmExternalContractorPaymentAsync(dto));
            Assert.Contains("Không tìm thấy transaction", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        #endregion
    }
}