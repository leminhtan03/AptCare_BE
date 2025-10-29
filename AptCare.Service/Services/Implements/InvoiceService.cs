using AptCare.Repository.Entities;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Repository.Enum;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AptCare.Service.Dtos.InvoiceDtos;

namespace AptCare.Service.Services.Implements
{
    public class InvoiceService : BaseService<InvoiceService>, IInvoiceService
    {
        private readonly IUserContext _userContext;

        public InvoiceService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<InvoiceService> logger,
            IMapper mapper,
            IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
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
                predicate: x => x.RepairRequestId == repairRequestId && x.Status == ActiveStatus.Active,
                include: i => i.Include(x => x.InvoiceAccessories)
                               .Include(x => x.InvoiceServices)
                );

            return invoices;
        }
    }
}
