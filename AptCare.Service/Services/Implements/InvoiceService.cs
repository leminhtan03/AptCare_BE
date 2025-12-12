using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace AptCare.Service.Services.Implements
{
    public class InvoiceService : BaseService<InvoiceService>, IInvoiceService
    {
        private readonly IUserContext _userContext;
        private readonly IS3FileService _s3FileService;
        private readonly ICloudinaryService _cloudinaryService;

        public InvoiceService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InvoiceService> logger, IMapper mapper, IUserContext userContext, IS3FileService s3FileService, ICloudinaryService cloudinaryService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _s3FileService = s3FileService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<string> CreateInternalInvoiceAsync(InvoiceInternalCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var repairRequest = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    predicate: p => p.RepairRequestId == dto.RepairRequestId,
                    include: i => i.Include(x => x.RequestTrackings)
                );
                if (repairRequest == null)
                {
                    throw new AppValidationException($"Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                }
                var lastStatusRequest = repairRequest.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status;
                if (lastStatusRequest != RequestStatus.InProgress)
                {
                    throw new AppValidationException($"Trạng thái sửa chữa đang là {lastStatusRequest}.", StatusCodes.Status404NotFound);
                }

                //var lastInspec = await _unitOfWork.GetRepository<InspectionReport>().SingleOrDefaultAsync(
                //    predicate: p => p.Appointment.RepairRequestId == dto.RepairRequestId,
                //    include: i => i.Include(x => x.Appointment),
                //    orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                //    );
                //if (lastInspec == null)
                //{
                //    throw new AppValidationException($"Chưa có báo cáo kiểm tra.", StatusCodes.Status404NotFound);
                //}
                //if (lastInspec.Status != ReportStatus.Approved)
                //{
                //    throw new AppValidationException($"Báo cáo kiểm tra chưa được chấp thuận.");
                //}

                // Tạo invoice chính
                var mainInvoice = _mapper.Map<Invoice>(dto);
                decimal mainInvoiceTotalAmount = 0;

                if (dto.AvailableAccessories != null && dto.AvailableAccessories.Count > 0)
                {
                    foreach (var accessory in dto.AvailableAccessories)
                    {
                        var accessoryDb = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                            predicate: p => p.AccessoryId == accessory.AccessoryId && p.Status == ActiveStatus.Active
                        );

                        if (accessoryDb == null)
                        {
                            throw new AppValidationException($"Vật tư với ID {accessory.AccessoryId} không tồn tại.", StatusCodes.Status404NotFound);
                        }

                        if (accessoryDb.Quantity < accessory.Quantity)
                        {
                            throw new AppValidationException($"Vật tư '{accessoryDb.Name}' không đủ số lượng. Còn: {accessoryDb.Quantity}, Cần: {accessory.Quantity}. " +
                                $"Vui lòng thêm vào danh sách Linh kiện cần mua để mua thêm.",
                                StatusCodes.Status400BadRequest);
                        }

                        mainInvoiceTotalAmount += accessoryDb.Price * accessory.Quantity;

                        mainInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            AccessoryId = accessory.AccessoryId,
                            Name = accessoryDb.Name,
                            Quantity = accessory.Quantity,
                            Price = accessoryDb.Price
                        });
                    }
                }

                if (dto.Services != null && dto.Services.Count > 0)
                {
                    foreach (var service in dto.Services)
                    {
                        mainInvoiceTotalAmount += service.Price;

                        mainInvoice.InvoiceServices.Add(new Repository.Entities.InvoiceService
                        {
                            Name = service.Name,
                            Price = service.Price,
                        });
                    }
                }



                decimal purchaseAmount = 0;
                Invoice? purchaseInvoice = null;

                if (dto.AccessoriesToPurchase != null && dto.AccessoriesToPurchase.Count > 0)
                {
                    purchaseInvoice = new Invoice
                    {
                        RepairRequestId = dto.RepairRequestId,
                        Type = InvoiceType.AccessoryPurchase,
                        Status = InvoiceStatus.Draft,
                        IsChargeable = false,
                        InvoiceAccessories = new List<InvoiceAccessory>(),
                        InvoiceServices = new List<Repository.Entities.InvoiceService>(),
                        CreatedAt = DateTime.Now
                    };

                    foreach (var accessory in dto.AccessoriesToPurchase)
                    {
                        var accessoryDb = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                            predicate: p => p.AccessoryId == accessory.AccessoryId && p.Status == ActiveStatus.Active
                        );

                        if (accessoryDb == null)
                        {
                            throw new AppValidationException($"Vật tư với ID {accessory.AccessoryId} không tồn tại trong hệ thống.", StatusCodes.Status404NotFound);
                        }

                        purchaseAmount += accessory.PurchasePrice * accessory.Quantity;
                        mainInvoiceTotalAmount += accessory.PurchasePrice * accessory.Quantity;
                        purchaseInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            AccessoryId = accessory.AccessoryId,
                            Name = accessory.Name,
                            Quantity = accessory.Quantity,
                            Price = accessory.PurchasePrice
                        });
                        mainInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            AccessoryId = accessory.AccessoryId,
                            Name = accessory.Name,
                            Quantity = accessory.Quantity,
                            Price = accessory.PurchasePrice
                        });
                    }

                    purchaseInvoice.TotalAmount = purchaseAmount;
                    await _unitOfWork.GetRepository<Invoice>().InsertAsync(purchaseInvoice);
                }
                mainInvoice.TotalAmount = mainInvoiceTotalAmount;
                await _unitOfWork.GetRepository<Invoice>().InsertAsync(mainInvoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                //if (purchaseInvoice != null)
                //{
                //    var purchaseDetails = string.Join(", ", dto.AccessoriesToPurchase!.Select(a =>
                //        $"{a.Name} ({a.Quantity} x {a.PurchasePrice:N0}đ = {a.Quantity * a.PurchasePrice:N0}đ)"));

                //    return $"Tạo biên lai sửa chữa thành công.\n\n" +
                //           $"Biên lai chính:\n" +
                //           $"   - Vật tư từ kho: {dto.AvailableAccessories?.Count ?? 0} loại\n" +
                //           $"   - Dịch vụ: {dto.Services?.Count ?? 0} loại\n" +
                //           $"   - Tổng: {mainInvoiceTotalAmount:N0}đ\n\n" +
                //           $"Biên lai mua vật tư:\n" +
                //           $"   - {purchaseDetails}\n" +
                //           $"   - Tổng chi phí mua: {purchaseAmount:N0}đ\n" +
                //           $"   - Trạng thái: Chờ phê duyệt\n\n" +
                //           $"Biên lai mua vật tư sẽ được xử lý khi Manager/TechLead phê duyệt InspectionReport.";
                //}

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

                var repairRequest = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    predicate: p => p.RepairRequestId == dto.RepairRequestId,
                    include: i => i.Include(x => x.RequestTrackings)
                );

                if (repairRequest == null)
                {
                    throw new AppValidationException($"Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                }

                var lastStatusRequest = repairRequest.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status;
                if (lastStatusRequest != RequestStatus.InProgress)
                {
                    throw new AppValidationException($"Trạng thái sửa chữa đang là {lastStatusRequest}.", StatusCodes.Status404NotFound);
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
                predicate: x => x.RepairRequestId == repairRequestId &&
                               x.Status != InvoiceStatus.Draft &&
                               x.Status != InvoiceStatus.Cancelled,
                include: i => i.Include(x => x.InvoiceAccessories)
                               .Include(x => x.InvoiceServices)
            );
            foreach (var invoice in invoices)
            {
                var medias = await _unitOfWork.GetRepository<Media>().ProjectToListAsync<MediaDto>(
                    configuration: _mapper.ConfigurationProvider,
                    predicate: m => m.EntityId == invoice.InvoiceId && m.Entity == nameof(Invoice) && m.Status == ActiveStatus.Active
                );
                invoice.Medias = (List<MediaDto>?)medias;
            }


            return invoices;
        }

        public async Task<string> ConfirmExternalContractorPaymentAsync(ExternalContractorPaymentConfirmDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == dto.InvoiceId &&
                                    x.Type == InvoiceType.ExternalContractor &&
                                    x.Status == InvoiceStatus.Approved,
                    include: i => i.Include(x => x.InvoiceAccessories)
                                   .Include(x => x.InvoiceServices)
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy invoice thuê ngoài hoặc invoice chưa được phê duyệt.", StatusCodes.Status404NotFound);

                if (invoice.IsChargeable)
                    throw new AppValidationException("Invoice này do cư dân trả, không cần xác nhận thanh toán cho nhà thầu.", StatusCodes.Status400BadRequest);

                var transaction = await transactionRepo.SingleOrDefaultAsync(
                    predicate: t => t.InvoiceId == invoice.InvoiceId &&
                                    t.Status == TransactionStatus.Pending &&
                                    t.Direction == TransactionDirection.Expense
                );

                if (transaction == null)
                    throw new AppValidationException("Không tìm thấy transaction pending cho invoice này.", StatusCodes.Status404NotFound);

                transaction.Status = TransactionStatus.Success;
                transaction.PaidAt = DateTime.Now;
                transaction.Description += $"\nĐã thanh toán thực tế lúc {DateTime.Now:dd/MM/yyyy HH:mm}.\nGhi chú: {dto.Note}";
                transactionRepo.UpdateAsync(transaction);

                if (dto.PaymentReceipt != null && dto.PaymentReceipt.Length > 0)
                {
                    string? fileKey = null;

                    if (dto.PaymentReceipt.ContentType.ToLower().Contains("application/pdf"))
                    {
                        fileKey = await _s3FileService.UploadFileAsync(
                            dto.PaymentReceipt,
                            $"transactions/external-payments/{invoice.InvoiceId}/");
                    }
                    else if (new[] { "image/jpeg", "image/png", "image/jpg" }
                        .Contains(dto.PaymentReceipt.ContentType.ToLower()))
                    {
                        fileKey = await _cloudinaryService.UploadImageAsync(dto.PaymentReceipt);
                    }

                    if (!string.IsNullOrEmpty(fileKey))
                    {
                        var media = new Media
                        {
                            EntityId = dto.InvoiceId,
                            Entity = nameof(Invoice),
                            FileName = dto.PaymentReceipt.FileName,
                            FilePath = fileKey,
                            ContentType = dto.PaymentReceipt.ContentType,
                            Status = ActiveStatus.Active,
                            CreatedAt = DateTime.Now
                        };
                        await mediaRepo.InsertAsync(media);
                    }
                }

                invoice.Status = InvoiceStatus.Paid;
                invoiceRepo.UpdateAsync(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return $"Xác nhận đã thanh toán cho nhà thầu thành công.\n";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e, "Error confirming external contractor payment for invoice {InvoiceId}", dto.InvoiceId);
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
