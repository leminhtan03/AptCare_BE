using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class InvoiceService : BaseService<InvoiceService>, IInvoiceService
    {
        private readonly IUserContext _userContext;

        public InvoiceService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<InvoiceService> logger, IMapper mapper, IUserContext userContext, PayOS.PayOSClient @object, Microsoft.Extensions.Options.IOptions<PayOS.PayOSOptions> object1) : base(unitOfWork, logger, mapper)
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
                predicate: x => x.RepairRequestId == repairRequestId && x.Status != InvoiceStatus.Draft && x.Status != InvoiceStatus.Cancelled,
                include: i => i.Include(x => x.InvoiceAccessories)
                               .Include(x => x.InvoiceServices)
                );

            return invoices;
        }
    }
}
