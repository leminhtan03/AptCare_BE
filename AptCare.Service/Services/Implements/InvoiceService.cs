using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.PayOSService;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class InvoiceService : BaseService<InvoiceService>, IInvoiceService
    {
        private readonly IUserContext _userContext;
        private readonly IPayOSClient _payOSClient;
        private readonly PayOSOptions _payOSOptions;

        public InvoiceService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InvoiceService> logger, IMapper mapper, IUserContext userContext, IPayOSClient payOSClient, IOptions<PayOSOptions> payOSOptions) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _payOSClient = payOSClient;
            _payOSOptions = payOSOptions.Value;
        }
        public async Task<string> CreateInternalInvoiceAsync(InvoiceInternalCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                                    predicate: p => p.RepairRequestId == dto.RepairRequestId
                                    );
                if (!isExistingRepairRequest)
                {
                    throw new AppValidationException($"Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                }

                var invoice = _mapper.Map<Invoice>(dto);

                decimal totalAmount = 0;

                if (dto.Accessories != null && dto.Accessories.Count != 0)
                {
                    foreach (var accessory in dto.Accessories)
                    {
                        var accessoryDb = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                                    predicate: p => p.AccessoryId == accessory.AccessoryId && p.Status == ActiveStatus.Active
                        );
                        if (accessoryDb == null)
                        {
                            throw new AppValidationException($"Phụ kiện không tồn tại.", StatusCodes.Status404NotFound);
                        }
                        if (accessoryDb.Quantity < accessory.Quantity)
                        {
                            throw new AppValidationException($"Phụ kiện trong kho không đủ số lượng.");
                        }

                        totalAmount += accessoryDb.Price * accessory.Quantity;
                        accessoryDb.Quantity -= accessory.Quantity;

                        _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessoryDb);

                        invoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            AccessoryId = accessory.AccessoryId,
                            Name = accessoryDb.Name,
                            Quantity = accessory.Quantity,
                            Price = accessoryDb.Price
                        });
                    }
                }

                if (dto.Services != null && dto.Services.Count != 0)
                {
                    foreach (var service in dto.Services)
                    {
                        totalAmount += service.Price;

                        invoice.InvoiceServices.Add(new Repository.Entities.InvoiceService
                        {
                            Name = service.Name,
                            Price = service.Price,
                        });
                    }
                }

                invoice.TotalAmount = totalAmount;

                await _unitOfWork.GetRepository<Invoice>().InsertAsync(invoice);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return "Tạo biên lai sửa chữa thành công.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<string> CreateExternalInvoiceAsync(InvoiceExternalCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                                    predicate: p => p.RepairRequestId == dto.RepairRequestId
                                    );
                if (!isExistingRepairRequest)
                {
                    throw new AppValidationException($"Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                }

                var invoice = _mapper.Map<Invoice>(dto);

                decimal totalAmount = 0;

                if (dto.Accessories != null && dto.Accessories.Count != 0)
                {
                    foreach (var accessory in dto.Accessories)
                    {
                        totalAmount += accessory.Price * accessory.Quantity;
                        invoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            Name = accessory.Name,
                            Quantity = accessory.Quantity,
                            Price = accessory.Price
                        });
                    }
                }

                if (dto.Services != null && dto.Services.Count != 0)
                {
                    foreach (var service in dto.Services)
                    {
                        totalAmount += service.Price;

                        invoice.InvoiceServices.Add(new Repository.Entities.InvoiceService
                        {
                            Name = service.Name,
                            Price = service.Price,
                        });
                    }
                }

                invoice.TotalAmount = totalAmount;

                await _unitOfWork.GetRepository<Invoice>().InsertAsync(invoice);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return "Tạo biên lai bên thứ 3 thành công.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<IEnumerable<InvoiceDto>> GetInvoicesAsync(int repairRequestId)
        {
            var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                        predicate: x => x.RepairRequestId == repairRequestId
                        );
            if (!isExistingRepairRequest)
            {
                throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
            }

            var invoices = await _unitOfWork.GetRepository<Invoice>().ProjectToListAsync<InvoiceDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: x => x.RepairRequestId == repairRequestId && x.Status != InvoiceStatus.Cancelled,
                include: i => i.Include(x => x.InvoiceAccessories)
                               .Include(x => x.InvoiceServices)
                );

            return invoices;
        }

        public async Task<string> CreateInvoicePaymentLinkAsync(int invoiceId)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var repairRepo = _unitOfWork.GetRepository<RepairRequest>();
            var txRepo = _unitOfWork.GetRepository<Transaction>();

            var invoice = await invoiceRepo.SingleOrDefaultAsync(predicate: x => x.InvoiceId == invoiceId)
                ?? throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

            if (invoice.TotalAmount <= 0)
                throw new AppValidationException("Hóa đơn không có giá trị, không thể tạo thanh toán.", StatusCodes.Status400BadRequest);

            var repair = await repairRepo.SingleOrDefaultAsync(predicate: r => r.RepairRequestId == invoice.RepairRequestId)
                ?? throw new AppValidationException("Không tìm thấy yêu cầu sửa chữa.", StatusCodes.Status404NotFound);

            switch (invoice.Type)
            {
                case InvoiceType.InternalRepair:
                    if (!invoice.IsChargeable)
                    {
                        throw new AppValidationException(
                            "Hóa đơn nội bộ này là chi phí nội bộ, không thu cư dân. Không được phép tạo link PayOS.",
                            StatusCodes.Status400BadRequest);
                    }
                    if (repair.RequestTrackings != null && repair.RequestTrackings.LastOrDefault().Status != RequestStatus.AcceptancePendingVerify)
                        throw new AppValidationException(
                            "Công việc sửa chữa nội bộ chưa hoàn tất, chưa thể tạo link thanh toán.",
                            StatusCodes.Status400BadRequest);
                    break;

                case InvoiceType.ExternalContractor:
                    if (!invoice.IsChargeable)
                    {
                        throw new AppValidationException(
                            "Hóa đơn nhà thầu này chỉ là khoản CHI cho nhà thầu, không thu cư dân. Không được phép tạo link PayOS.",
                            StatusCodes.Status400BadRequest);
                    }
                    if (repair.RequestTrackings != null && repair.RequestTrackings.LastOrDefault().Status != RequestStatus.AcceptancePendingVerify)
                        throw new AppValidationException(
                            "Công việc do nhà thầu thực hiện chưa hoàn tất, chưa thể thu cư dân.",
                            StatusCodes.Status400BadRequest);
                    break;

                default:
                    throw new AppValidationException("Loại hóa đơn không hợp lệ.", StatusCodes.Status400BadRequest);
            }

            var successIncome = await txRepo.GetListAsync(
                predicate: t => t.InvoiceId == invoiceId
                             && t.Direction == TransactionDirection.Income
                             && t.Status == TransactionStatus.Success);

            var totalPaid = successIncome.Sum(t => (decimal?)t.Amount) ?? 0m;
            var remaining = invoice.TotalAmount - totalPaid;
            if (remaining <= 0)
                throw new AppValidationException("Hóa đơn đã được thanh toán đủ, không thể tạo thêm link.", StatusCodes.Status400BadRequest);

            var orderCode = GenerateOrderCode(invoiceId);

            var tx = new Transaction
            {
                UserId = invoice.RepairRequest.UserId,
                InvoiceId = invoiceId,
                Direction = TransactionDirection.Income,
                TransactionType = TransactionType.Payment,
                Status = TransactionStatus.Pending,
                Provider = PaymentProvider.PayOS,
                Amount = remaining,
                Description = $"Thanh toán hóa đơn sửa chữa #{invoiceId}",
                CreatedAt = DateTime.UtcNow,
                PayOSOrderCode = orderCode
            };

            await txRepo.InsertAsync(tx);
            await _unitOfWork.CommitAsync();

            var (checkoutUrl, _) = await _payOSClient.CreatePaymentLinkAsync(
                orderCode,
                (long)Math.Round(remaining),
                tx.Description,
                _payOSOptions.ReturnUrl);

            tx.PayOSCheckoutUrl = checkoutUrl;
            txRepo.UpdateAsync(tx);
            await _unitOfWork.CommitAsync();

            return checkoutUrl;
        }

        private long GenerateOrderCode(int invoiceId)
        {
            var now = DateTime.UtcNow;
            var prefix = long.Parse(now.ToString("yyMMddHHmmss"));
            return prefix * 1000 + (invoiceId % 1000);
        }
        public async Task CreateContractorDepositAsync(int invoiceId, decimal depositAmount, string contractorInvoiceFileUrl)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var repairRepo = _unitOfWork.GetRepository<RepairRequest>();
            var txRepo = _unitOfWork.GetRepository<Transaction>();
            var mediaRepo = _unitOfWork.GetRepository<Media>(); // bảng media của bạn

            var invoice = await invoiceRepo.SingleOrDefaultAsync(predicate: x => x.InvoiceId == invoiceId)
                ?? throw new AppValidationException("Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound);

            if (invoice.Type != InvoiceType.ExternalContractor)
                throw new AppValidationException("Chỉ hóa đơn nhà thầu mới được đặt cọc.", StatusCodes.Status400BadRequest);

            var repair = await repairRepo.SingleOrDefaultAsync(predicate: r => r.RepairRequestId == invoice.RepairRequestId)
                ?? throw new AppValidationException("Không tìm thấy yêu cầu sửa chữa.", StatusCodes.Status404NotFound);

            if (repair.RequestTrackings != null && repair.RequestTrackings.LastOrDefault().Status != RequestStatus.Scheduling &&
                repair.RequestTrackings.LastOrDefault().Status != RequestStatus.InProgress &&
                 repair.RequestTrackings.LastOrDefault().Status != RequestStatus.Approved)
            {
                throw new AppValidationException("Chỉ đặt cọc khi yêu cầu đã được duyệt / đang thực hiện.", StatusCodes.Status400BadRequest);
            }

            if (depositAmount <= 0)
                throw new AppValidationException("Số tiền đặt cọc phải lớn hơn 0.", StatusCodes.Status400BadRequest);

            var expenseTransactions = await txRepo.GetListAsync(
                predicate: t => t.InvoiceId == invoiceId &&
                                t.Direction == TransactionDirection.Expense &&
                                t.Status == TransactionStatus.Success);

            var totalExpense = expenseTransactions.Sum(t => (decimal?)t.Amount) ?? 0m;

            if (totalExpense + depositAmount > invoice.TotalAmount)
                throw new AppValidationException("Tổng tiền chi (bao gồm lần cọc này) vượt quá giá trị hóa đơn.", StatusCodes.Status400BadRequest);

            // Đếm xem đây là lần đặt cọc thứ mấy
            var depositCount = (await txRepo.GetListAsync(
                predicate: t => t.InvoiceId == invoiceId &&
                                t.Direction == TransactionDirection.Expense &&
                                t.Description.StartsWith("Đặt cọc"))).Count;

            var depositIndex = depositCount + 1;

            var tx = new Transaction
            {
                UserId = _userContext.CurrentUserId, // Manager đang chi
                InvoiceId = invoiceId,
                Direction = TransactionDirection.Expense,
                TransactionType = TransactionType.Cash,
                Status = TransactionStatus.Success,
                Provider = PaymentProvider.UnKnow,
                Amount = depositAmount,
                Description = $"Đặt cọc lần {depositIndex} cho nhà thầu #{invoiceId}",
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };

            await txRepo.InsertAsync(tx);

            // Lưu file hóa đơn nhà thầu gắn với Invoice
            //if (!string.IsNullOrWhiteSpace(contractorInvoiceFileUrl))
            //{
            //    var media = new Media
            //    {
            //        EntityId = invoiceId,
            //        Entity = MediaEntity.InvoiceContractor,      // enum của bạn
            //        FileName = Path.GetFileName(contractorInvoiceFileUrl),
            //        FilePath = contractorInvoiceFileUrl,
            //        ContentType = "application/pdf",             // hoặc truyền từ ngoài vào
            //        CreatedAt = DateTime.UtcNow,
            //        Status = ActiveStatus.Active
            //    };
            //    await mediaRepo.InsertAsync(media);
            //}

            await _unitOfWork.CommitAsync();
        }
    }
}
