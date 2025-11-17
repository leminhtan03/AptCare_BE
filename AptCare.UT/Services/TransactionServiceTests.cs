using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using System.Linq.Expressions;

namespace AptCare.UT.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepo = new();
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IS3FileService> _s3FileService = new();
        private readonly Mock<PayOSClient> _payOSClient;
        private readonly Mock<IOptions<PayOSOptions>> _payOSOptions = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<TransactionService>> _logger = new();

        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            var options = new PayOSOptions
            {
                BaseUrl = "https://test.payos.vn",
                ClientId = "test-client",
                ApiKey = "test-key",
                ChecksumKey = "test-checksum"
            };

            _payOSOptions.Setup(o => o.Value).Returns(options);

            // Create PayOSClient with actual constructor parameters
            _payOSClient = new Mock<PayOSClient>("test-client", "test-key", "test-checksum");

            _uow.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new TransactionService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _s3FileService.Object,
                (ICloudinaryService)_payOSClient.Object,
                _payOSOptions.Object
            );
        }

        #region CreateIncomePaymentLinkAsync Tests

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Success_ReturnsPaymentLink()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.Draft,
                RepairRequest = new RepairRequest
                {
                    UserId = 1,
                    RequestTrackings = new List<RequestTracking>
                    {
                        new RequestTracking
                        {
                            Status = RequestStatus.Completed,
                            UpdatedAt = DateTime.Now
                        }
                    }
                }
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync((Transaction)null);

            var paymentLinkResponse = new CreatePaymentLinkResponse
            {
                CheckoutUrl = "https://payos.com/pay/123",
                PaymentLinkId = "link-id-123",
                OrderCode = 123456
            };

            _payOSClient.Setup(p => p.PaymentRequests.CreateAsync(
                It.IsAny<CreatePaymentLinkRequest>(),
                It.IsAny<RequestOptions<CreatePaymentLinkRequest>>()
            )).ReturnsAsync(paymentLinkResponse);

            Transaction insertedTransaction = null;
            _transactionRepo.Setup(r => r.InsertAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => insertedTransaction = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateIncomePaymentLinkAsync(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("https://payos.com", result);
            Assert.Equal(InvoiceStatus.AwaitingPayment, invoice.Status);
            Assert.NotNull(insertedTransaction);
            Assert.Equal("https://payos.com/pay/123", insertedTransaction.CheckoutUrl);
            _transactionRepo.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Once);
            _payOSClient.Verify(p => p.PaymentRequests.CreateAsync(
                It.IsAny<CreatePaymentLinkRequest>(),
                It.IsAny<RequestOptions<CreatePaymentLinkRequest>>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenNotChargeable()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = false, // Not chargeable
                TotalAmount = 5000000
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("không thu phí", ex.Message);
        }

        #endregion

        #region GetTransactionByIdAsync Tests

        [Fact]
        public async Task GetTransactionByIdAsync_Success_ReturnsTransaction()
        {
            // Arrange
            var transactionId = 1;
            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Amount = 1000000,
                User = new User
                {
                    UserId = 1,
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Amount = 1000000
            };

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transaction);

            _mapper.Setup(m => m.Map<TransactionDto>(transaction)).Returns(transactionDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((Media)null);

            // Act
            var result = await _service.GetTransactionByIdAsync(transactionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(transactionId, result.TransactionId);
            Assert.Equal("John Doe", result.UserFullName);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            var transactionId = 999;

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync((Transaction)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetTransactionByIdAsync(transactionId));
            Assert.Contains("Không tìm thấy giao dịch", ex.Message);
        }

        #endregion
    }
}