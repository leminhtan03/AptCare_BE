using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Service.Dtos.AccessoryDto;

namespace AptCare.Service.Services.Implements
{
    public class AccessoryService : BaseService<AccessoryService>, IAccessoryService
    {
        public AccessoryService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<AccessoryService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateAccessoryAsync(AccessoryCreateDto dto)
        {
            try
            {
                var isDup = await _unitOfWork.GetRepository<Accessory>().AnyAsync(
                    predicate: x => x.Name == dto.Name
                );
                if (isDup)
                    throw new AppValidationException("Phụ kiện đã tồn tại.");

                var accessory = _mapper.Map<Accessory>(dto);
                await _unitOfWork.GetRepository<Accessory>().InsertAsync(accessory);
                await _unitOfWork.CommitAsync();
                return "Tạo phụ kiện thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateAccessoryAsync(int id, AccessoryUpdateDto dto)
        {
            try
            {
                var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                    predicate: x => x.AccessoryId == id
                );
                if (accessory == null)
                    throw new AppValidationException("Phụ kiện không tồn tại.", StatusCodes.Status404NotFound);

                var isDup = await _unitOfWork.GetRepository<Accessory>().AnyAsync(
                    predicate: x => x.Name == dto.Name && x.AccessoryId != id
                );
                if (isDup)
                    throw new AppValidationException("Tên phụ kiện đã tồn tại.");

                _mapper.Map(dto, accessory);
                _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                await _unitOfWork.CommitAsync();
                return "Cập nhật phụ kiện thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteAccessoryAsync(int id)
        {
            try
            {
                var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                    predicate: x => x.AccessoryId == id
                );
                if (accessory == null)
                    throw new AppValidationException("Phụ kiện không tồn tại.", StatusCodes.Status404NotFound);

                _unitOfWork.GetRepository<Accessory>().DeleteAsync(accessory);
                await _unitOfWork.CommitAsync();
                return "Xóa phụ kiện thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<AccessoryDto> GetAccessoryByIdAsync(int id)
        {
            var accessory = await _unitOfWork.GetRepository<Accessory>().ProjectToSingleOrDefaultAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: p => p.AccessoryId == id
            );

            if (accessory == null)
                throw new AppValidationException("Phụ kiện không tồn tại.", StatusCodes.Status404NotFound);

            return accessory;
        }

        public async Task<IPaginate<AccessoryDto>> GetPaginateAccessoryAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Accessory, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Name.ToLower().Contains(search) || (p.Descrption != null && p.Descrption.ToLower().Contains(search))) &&
                (string.IsNullOrEmpty(filter) || (filter == "active" && p.Status == ActiveStatus.Active) || (filter == "inactive" && p.Status == ActiveStatus.Inactive));

            var result = await _unitOfWork.GetRepository<Accessory>().ProjectToPagingListAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                page: page,
                size: size
            );

            return result;
        }

        public async Task<IEnumerable<AccessoryDto>> GetAccessoriesAsync()
        {
            var result = await _unitOfWork.GetRepository<Accessory>().ProjectToListAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: p => p.Status == ActiveStatus.Active,
                orderBy: o => o.OrderBy(x => x.Name)
            );

            return result;
        }

        private Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.AccessoryId);

            return sortBy.ToLower() switch
            {
                "name" => q => q.OrderBy(p => p.Name),
                "name_desc" => q => q.OrderByDescending(p => p.Name),
                "price" => q => q.OrderBy(p => p.Price),
                "price_desc" => q => q.OrderByDescending(p => p.Price),
                _ => q => q.OrderByDescending(p => p.AccessoryId)
            };
        }
    }
}
