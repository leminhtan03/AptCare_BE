using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class InvoiceService : BaseService<InvoiceService>, IInvoiceService
    {
        private readonly IUserContext _userContext;

        public InvoiceService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InvoiceService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
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
                        InvoiceServices = new List<Repository.Entities.InvoiceService>()
                    };
                    purchaseInvoice.TotalAmount = purchaseAmount;
                    await _unitOfWork.GetRepository<Invoice>().InsertAsync(purchaseInvoice);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                //if (purchaseInvoice != null)
                //{
                //    var purchaseDetails = string.Join(", ", dto.AccessoriesToPurchase!.Select(a =>
                //        $"{a.Name} ({a.Quantity} x {a.PurchasePrice:N0}đ = {a.Quantity * a.PurchasePrice:N0}đ)"));

                //    return $"Tạo biên lai sửa chữa thành công.\n\n" +
                //           $"Biên lai chính:\n" +
                //           $"   - Phụ kiện từ kho: {dto.AvailableAccessories?.Count ?? 0} loại\n" +
                //           $"   - Dịch vụ: {dto.Services?.Count ?? 0} loại\n" +
                //           $"   - Tổng: {mainInvoiceTotalAmount:N0}đ\n\n" +
                //           $"Biên lai mua phụ kiện:\n" +
                //           $"   - {purchaseDetails}\n" +
                //           $"   - Tổng chi phí mua: {purchaseAmount:N0}đ\n" +
                //           $"   - Trạng thái: Chờ phê duyệt\n\n" +
                //           $"Biên lai mua phụ kiện sẽ được xử lý khi Manager/TechLead phê duyệt InspectionReport.";
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

                //var lastInspec = await _unitOfWork.GetRepository<InspectionReport>().SingleOrDefaultAsync(
                //    predicate: p => p.Appointment.RepairRequestId == dto.RepairRequestId,
                //    include: i => i.Include(x => x.Appointment),
                //    orderBy: o => o.OrderByDescending(x => x.CreatedAt)
                //    );
                //if (lastInspec == null)
                //{
                //    throw new AppValidationException($"Chưa có báo cáo kiểm tra.", StatusCodes.Status404NotFound);
                //}
                //if (lastInspec.SolutionType != SolutionType.Outsource)
                //{
                //    throw new AppValidationException($"Phương pháp sửa chữa không phải là thuê ngoài.");
                //}
                //if (lastInspec.Status != ReportStatus.Approved)
                //{
                //    throw new AppValidationException($"Báo cáo kiểm tra chưa được chấp thuận.");
                //}

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

            return invoices;
        }
    }
}
