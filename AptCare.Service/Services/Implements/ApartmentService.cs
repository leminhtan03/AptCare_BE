using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AptCare.Repository.Enum;

namespace AptCare.Service.Services.Implements
{
    public class ApartmentService : BaseService<ApartmentService>, IApartmentService
    {
        public ApartmentService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<ApartmentService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateApartmentAsync(ApartmentCreateDto dto)
        {
            try
            {
                var floor = await _unitOfWork.GetRepository<Floor>().SingleOrDefaultAsync(
                    predicate: x => x.FloorId == dto.FloorId
                    );
                if (floor == null)
                {
                    throw new Exception("Tầng không tồn tại.");
                }
                if (floor.Status == ActiveStatus.Inactive)
                {
                    throw new Exception("Tầng đã ngưng hoạt động.");
                }

                var isDupApartment = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                    predicate: x => x.RoomNumber == dto.RoomNumber
                    );
                if (isDupApartment)
                {
                    throw new Exception("Số phòng đã tồn tại.");
                }

                var apartment = _mapper.Map<Apartment>(dto);

                await _unitOfWork.GetRepository<Apartment>().InsertAsync(apartment);
                await _unitOfWork.CommitAsync();
                return "Taọ căn hộ mới thành công";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> UpdateApartmentAsync(int id, ApartmentUpdateDto dto)
        {
            try
            {                
                var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                    predicate: x => x.ApartmentId == id
                    );
                if (apartment == null)
                {
                    throw new KeyNotFoundException("Căn hộ không tồn tại.");
                }

                var floor = await _unitOfWork.GetRepository<Floor>().SingleOrDefaultAsync(
                    predicate: x => x.FloorId == dto.FloorId
                    );
                if (floor == null)
                {
                    throw new Exception("Tầng không tồn tại.");
                }
                if (floor.Status == ActiveStatus.Inactive)
                {
                    throw new Exception("Tầng đã ngưng hoạt động.");
                }

                var isDupApartment = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                    predicate: x => x.RoomNumber == dto.RoomNumber
                    );
                if (isDupApartment)
                {
                    throw new Exception("Số phòng đã tồn tại.");
                }

                _mapper.Map(dto, apartment);
                _unitOfWork.GetRepository<Apartment>().UpdateAsync(apartment);
                await _unitOfWork.CommitAsync();
                return "Cập nhật căn hộ thành công";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> DeleteApartmentAsync(int id)
        {
            try
            {
                var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                    predicate: x => x.ApartmentId == id
                    );

                if (apartment == null)
                {
                    throw new KeyNotFoundException("Căn hộ không tồn tại.");
                }

                _unitOfWork.GetRepository<Apartment>().DeleteAsync(apartment);
                await _unitOfWork.CommitAsync();
                return "Xóa căn hộ thành công";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<ApartmentDto> GetApartmentByIdAsync(int id)
        {
            var Apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<ApartmentDto>(x),
                predicate: p => p.ApartmentId == id,
                include: i => i.Include(x => x.UserApartments)
                                    .ThenInclude(x => x.User)
                                        .ThenInclude(x => x.Account)
                );

            if (Apartment == null)
            {
                throw new KeyNotFoundException("Căn hộ không tồn tại");
            }

            return Apartment;
        }

        public async Task<IPaginate<ApartmentDto>> GetPaginateApartmentAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Apartment, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.RoomNumber.ToString().Contains(search) ||
                                                 p.Description.Contains(search)) &&
                (string.IsNullOrEmpty(filter) ||
                filter.Equals(p.Status.ToString().ToLower()));

            var result = await _unitOfWork.GetRepository<Apartment>().GetPagingListAsync(
                selector: x => _mapper.Map<ApartmentDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.UserApartments)
                                    .ThenInclude(x => x.User)
                                        .ThenInclude(x => x.Account),
                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            return result;
        }

        private Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.ApartmentId);

            return sortBy.ToLower() switch
            {
                "room" => q => q.OrderBy(p => p.RoomNumber),
                "room_desc" => q => q.OrderByDescending(p => p.RoomNumber),
                _ => q => q.OrderByDescending(p => p.RoomNumber) // Default sort
            };
        }
    }
}
