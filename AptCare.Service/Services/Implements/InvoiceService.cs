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

                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: p => p.RepairRequestId == dto.RepairRequestId,
                    include: i => i.Include(x => x.RequestTrackings)
                                   .Include(x => x.Invoices)
                                       .ThenInclude(inv => inv.AccessoryStockTransactions)
                );

                if (repairRequest == null)
                    throw new AppValidationException($"Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);

                var lastStatusRequest = repairRequest.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status;
                if (lastStatusRequest != RequestStatus.InProgress)
                    throw new AppValidationException($"Trạng thái sửa chữa đang là {lastStatusRequest}.", StatusCodes.Status404NotFound);

                var existingInvoices = repairRequest.Invoices
                    ?.Where(i => i.Type == InvoiceType.InternalRepair &&
                                (i.Status == InvoiceStatus.Draft ||
                                 i.Status == InvoiceStatus.Approved))
                    .ToList();

                if (existingInvoices != null && existingInvoices.Any())
                {
                    var warnings = new List<string>();

                    foreach (var oldInvoice in existingInvoices)
                    {
                        var hasStockTransactions = oldInvoice.AccessoryStockTransactions
                            ?.Any(st => st.Status == StockTransactionStatus.Approved ||
                                       st.Status == StockTransactionStatus.Completed) ?? false;

                        if (hasStockTransactions)
                        {
                            var stockOutCount = oldInvoice.AccessoryStockTransactions
                                .Count(st => st.Type == StockTransactionType.Export);

                            var stockInCount = oldInvoice.AccessoryStockTransactions
                                .Count(st => st.Type == StockTransactionType.Import);

                            warnings.Add($"Invoice #{oldInvoice.InvoiceId} (Status: {oldInvoice.Status}):");
                            if (stockOutCount > 0)
                                warnings.Add($"  - Đã có {stockOutCount} phiếu xuất kho (vật tư từ kho)");
                            if (stockInCount > 0)
                                warnings.Add($"  - Đã có {stockInCount} phiếu nhập kho (vật tư mua mới)");
                        }
                        else if (oldInvoice.Status == InvoiceStatus.Draft)
                        {
                            warnings.Add($"Invoice #{oldInvoice.InvoiceId} đang ở trạng thái Draft (chưa có phiếu xuất/nhập)");
                        }
                    }

                    if (warnings.Any())
                    {
                        var warningMessage = "CẢNH BÁO: Đã có invoice tồn tại cho yêu cầu này:\n\n" +
                                           string.Join("\n", warnings) + "\n\n" +
                                           "ĐỀ XUẤT:\n" +
                                           "- Nếu muốn tạo invoice mới với báo giá khác, vui lòng HỦY invoice cũ trước.";

                        _logger.LogWarning(warningMessage);
                        throw new AppValidationException(warningMessage, StatusCodes.Status409Conflict);
                    }
                }

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
                            throw new AppValidationException($"Vật tư với ID {accessory.AccessoryId} không tồn tại.", StatusCodes.Status404NotFound);

                        if (accessoryDb.Quantity < accessory.Quantity)
                        {
                            throw new AppValidationException(
                                $"Vật tư '{accessoryDb.Name}' không đủ số lượng.\n" +
                                $"Hiện có: {accessoryDb.Quantity}\n" +
                                $"Cần: {accessory.Quantity}\n" +
                                $"Thiếu: {accessory.Quantity - accessoryDb.Quantity}\n\n",
                                StatusCodes.Status400BadRequest);
                        }

                        mainInvoiceTotalAmount += accessoryDb.Price * accessory.Quantity;

                        mainInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                        {
                            AccessoryId = accessory.AccessoryId,
                            Name = accessoryDb.Name,
                            Quantity = accessory.Quantity,
                            Price = accessoryDb.Price,
                            SourceType = InvoiceAccessorySourceType.FromStock
                        });
                    }
                }

                if (dto.AccessoriesToPurchase != null && dto.AccessoriesToPurchase.Count > 0)
                {
                    foreach (var accessory in dto.AccessoriesToPurchase)
                    {
                        if (accessory.AccessoryId.HasValue && accessory.AccessoryId.Value > 0)
                        {
                            var accessoryDb = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                                predicate: p => p.AccessoryId == accessory.AccessoryId.Value
                            );

                            if (accessoryDb == null)
                                throw new AppValidationException(
                                    $"Vật tư với ID {accessory.AccessoryId} không tồn tại trong hệ thống.",
                                    StatusCodes.Status404NotFound);

                            if (accessoryDb.Status != ActiveStatus.Active && accessoryDb.Status != ActiveStatus.Darft)
                                throw new AppValidationException(
                                    $"Vật tư '{accessoryDb.Name}' không ở trạng thái có thể sử dụng (Status: {accessoryDb.Status}).",
                                    StatusCodes.Status400BadRequest);

                            mainInvoiceTotalAmount += accessory.PurchasePrice * accessory.Quantity;

                            mainInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                            {
                                AccessoryId = accessory.AccessoryId.Value,
                                Name = accessoryDb.Name,
                                Quantity = accessory.Quantity,
                                Price = accessory.PurchasePrice,
                                SourceType = InvoiceAccessorySourceType.ToBePurchased
                            });

                            _logger.LogInformation(
                                "Vật tư '{AccessoryName}' (ID: {AccessoryId}) được thêm vào invoice để mua thêm {Quantity} đơn vị.",
                                accessoryDb.Name,
                                accessory.AccessoryId.Value,
                                accessory.Quantity);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(accessory.Name))
                                throw new AppValidationException("Vật tư mới phải có tên (Name).", StatusCodes.Status400BadRequest);

                            if (accessory.PurchasePrice <= 0)
                                throw new AppValidationException($"Giá mua của vật tư '{accessory.Name}' phải lớn hơn 0.", StatusCodes.Status400BadRequest);

                            var isDuplicate = await _unitOfWork.GetRepository<Accessory>().AnyAsync(
                                predicate: a => a.Name.ToLower() == accessory.Name.ToLower()
                            );

                            if (isDuplicate)
                            {
                                throw new AppValidationException(
                                    $"Vật tư '{accessory.Name}' đã tồn tại trong hệ thống.\n" +
                                    $"Vui lòng chọn từ danh sách hoặc sử dụng tên khác.",
                                    StatusCodes.Status400BadRequest);
                            }

                            var newAccessory = new Accessory
                            {
                                Name = accessory.Name,
                                Descrption = $"Vật tư mua cho yêu cầu sửa chữa #{dto.RepairRequestId}",
                                Price = accessory.PurchasePrice,
                                Quantity = 0,
                                Status = ActiveStatus.Darft
                            };

                            await _unitOfWork.GetRepository<Accessory>().InsertAsync(newAccessory);
                            await _unitOfWork.CommitAsync();

                            mainInvoiceTotalAmount += accessory.PurchasePrice * accessory.Quantity;

                            mainInvoice.InvoiceAccessories.Add(new InvoiceAccessory
                            {
                                AccessoryId = newAccessory.AccessoryId,
                                Name = accessory.Name,
                                Quantity = accessory.Quantity,
                                Price = accessory.PurchasePrice,
                                SourceType = InvoiceAccessorySourceType.ToBePurchased
                            });

                            _logger.LogInformation(
                                "Vật tư mới '{AccessoryName}' (ID: {AccessoryId}) được tạo và thêm vào invoice.",
                                accessory.Name,
                                newAccessory.AccessoryId);
                        }
                    }
                }

                // ✅ DỊCH VỤ
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

                mainInvoice.TotalAmount = mainInvoiceTotalAmount;
                await _unitOfWork.GetRepository<Invoice>().InsertAsync(mainInvoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return "Tạo biên lai sửa chữa thành công.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e, "Error creating internal invoice for repair request {RepairRequestId}", dto.RepairRequestId);
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
                            Price = accessory.Price,
                            SourceType = InvoiceAccessorySourceType.ToBePurchased,
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
        public async Task<string> CancelInvoiceAsync(int invoiceId, string reason)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
                var accessoryRepo = _unitOfWork.GetRepository<Accessory>();
                var stockTxRepo = _unitOfWork.GetRepository<AccessoryStockTransaction>();
                var budgetRepo = _unitOfWork.GetRepository<Budget>(); // ✅ THÊM
                var transactionRepo = _unitOfWork.GetRepository<Transaction>(); // ✅ THÊM

                var invoice = await invoiceRepo.SingleOrDefaultAsync(
                    predicate: x => x.InvoiceId == invoiceId,
                    include: i => i.Include(x => x.InvoiceAccessories)
                                   .Include(x => x.AccessoryStockTransactions)
                );

                if (invoice == null)
                    throw new AppValidationException("Không tìm thấy invoice.", StatusCodes.Status404NotFound);
                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new AppValidationException("Invoice đã bị hủy trước đó.");
                if (invoice.Status == InvoiceStatus.Paid)
                    throw new AppValidationException("Không thể hủy invoice đã thanh toán.");
                if (invoice.Status == InvoiceStatus.AwaitingPayment)
                    throw new AppValidationException("Không thể hủy invoice đang chờ thanh toán.");


                // XỬ LÝ VẬT TƯ TỪ KHO (FromStock) - Phiếu xuất
                var stockOutTransactions = invoice.AccessoryStockTransactions
                    ?.Where(st => st.Type == StockTransactionType.Export)
                    .ToList() ?? new List<AccessoryStockTransaction>();

                foreach (var stockOut in stockOutTransactions)
                {
                    if (stockOut.Status == StockTransactionStatus.Approved ||
                        stockOut.Status == StockTransactionStatus.Completed)
                    {
                        var accessory = await accessoryRepo.SingleOrDefaultAsync(
                            predicate: a => a.AccessoryId == stockOut.AccessoryId
                        );
                        if (accessory != null)
                        {
                            accessory.Quantity += stockOut.Quantity;
                            accessoryRepo.UpdateAsync(accessory);
                        }
                        stockOut.Status = StockTransactionStatus.Cancelled;
                        stockOut.Note += $"\n[Hủy vào {DateTime.Now:dd/MM/yyyy HH:mm}] {reason}";
                        stockTxRepo.UpdateAsync(stockOut);
                    }
                }

                // XỬ LÝ VẬT TƯ CẦN MUA (ToBePurchased) - Phiếu nhập
                var stockInTransactions = invoice.AccessoryStockTransactions
                    ?.Where(st => st.Type == StockTransactionType.Import)
                    .ToList() ?? new List<AccessoryStockTransaction>();

                foreach (var stockIn in stockInTransactions)
                {
                    // đã confirm là nhập kho thì hoàn trả vật tư về kho vì trước đó ko cộng vào khi nhập mới tại lúc xác nhận vì còn để dung sửa chữa ngay
                    if (stockIn.Status == StockTransactionStatus.Completed)
                    {
                        var accessory = await accessoryRepo.SingleOrDefaultAsync(
                            predicate: a => a.AccessoryId == stockIn.AccessoryId
                        );

                        if (accessory != null)
                        {
                            accessory.Quantity += stockIn.Quantity;
                            accessoryRepo.UpdateAsync(accessory);
                        }
                    }

                    // Hủy phiếu nhập khi pending chưa xác nhận nhập kho
                    if (stockIn.Status == StockTransactionStatus.Pending)
                    {
                        stockIn.Status = StockTransactionStatus.Cancelled;
                        stockIn.Note += $"\n[Hủy vào {DateTime.Now:dd/MM/yyyy HH:mm}] {reason}";
                        stockTxRepo.UpdateAsync(stockIn);
                    }
                }

                invoice.Status = InvoiceStatus.Cancelled;
                invoiceRepo.UpdateAsync(invoice);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                return $"Đã hủy invoice và hoàn trả vật tư. Lý do: {reason}";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e, "Error cancelling invoice {InvoiceId}", invoiceId);
                throw;
            }
        }
    }

}
