using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class ApartmentService : BaseService<ApartmentService>, IApartmentService
    {
        public ApartmentService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<ApartmentService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateApartmentAsync(ApartmentCreateDto dto)
        {
            // validate floor
            var floor = await _unitOfWork.GetRepository<Floor>()
                .SingleOrDefaultAsync(predicate: x => x.FloorId == dto.FloorId);

            if (floor is null)
                throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);

            if (floor.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Tầng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                x => x.FloorId == dto.FloorId && x.Room == dto.Room);

            if (isDup)
                throw new AppValidationException("Số phòng đã tồn tại.", StatusCodes.Status409Conflict);

            var apartment = _mapper.Map<Apartment>(dto);

            await _unitOfWork.GetRepository<Apartment>().InsertAsync(apartment);
            await _unitOfWork.CommitAsync();
            return "Tạo căn hộ mới thành công";
        }

        public async Task<string> UpdateApartmentAsync(int id, ApartmentUpdateDto dto)
        {
            var apartment = await _unitOfWork.GetRepository<Apartment>()
                .SingleOrDefaultAsync(predicate: x => x.ApartmentId == id);

            if (apartment is null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            var floor = await _unitOfWork.GetRepository<Floor>()
                .SingleOrDefaultAsync(predicate: x => x.FloorId == dto.FloorId);

            if (floor is null)
                throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);

            if (floor.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Tầng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                x => x.ApartmentId != id &&
                     x.FloorId == dto.FloorId &&
                     x.Room == dto.Room);

            if (isDup)
                throw new AppValidationException("Số phòng đã tồn tại.", StatusCodes.Status409Conflict);

            _mapper.Map(dto, apartment);
            _unitOfWork.GetRepository<Apartment>().UpdateAsync(apartment);
            await _unitOfWork.CommitAsync();
            return "Cập nhật căn hộ thành công";
        }

        public async Task<string> DeleteApartmentAsync(int id)
        {
            var apartment = await _unitOfWork.GetRepository<Apartment>()
                .SingleOrDefaultAsync(predicate: x => x.ApartmentId == id);

            if (apartment is null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            _unitOfWork.GetRepository<Apartment>().DeleteAsync(apartment);
            await _unitOfWork.CommitAsync();
            return "Xóa căn hộ thành công";
        }

        public async Task<ApartmentDto> GetApartmentByIdAsync(int id)
        {
            var apt = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<ApartmentDto>(x),
                predicate: p => p.ApartmentId == id,
                include: i => i.Include(x => x.UserApartments)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Account));

            if (apt is null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            await LoadUserProfileImagesAsync(apt);

            return apt;
        }

        public async Task<IPaginate<ApartmentDto>> GetPaginateApartmentAsync(PaginateDto dto, int? floorId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            if (floorId != null)
            {
                var isExistingFloor = await _unitOfWork.GetRepository<Floor>().AnyAsync(predicate: x => x.FloorId == floorId);
                if (!isExistingFloor)
                    throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);
            }


            Expression<Func<Apartment, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Room.ToString().Contains(search) ||
                                         (p.Description != null && p.Description.Contains(search))) &&
                (string.IsNullOrEmpty(filter) ||
                filter.Equals(p.Status.ToString().ToLower()) &&
                floorId == null || p.FloorId == floorId);

            var result = await _unitOfWork.GetRepository<Apartment>().GetPagingListAsync(
                selector: s => _mapper.Map<ApartmentDto>(s),
                predicate: predicate,
                include: i => i.Include(x => x.UserApartments!)
                                .ThenInclude(x => x.User)
                                .ThenInclude(x => x.Account),
                orderBy: BuildOrderBy(dto.sortBy ?? string.Empty),
                    page: page,
                    size: size
                );
            foreach (var apartment in result.Items)
                await LoadUserProfileImagesAsync(apartment);
            return result;
        }

        public async Task<IEnumerable<ApartmentDto>> GetApartmentsByFloorAsync(int floorId)
        {
            var result = await _unitOfWork.GetRepository<Apartment>().GetListAsync(
                selector: s => _mapper.Map<ApartmentDto>(s),
                predicate: p => p.FloorId == floorId,
                orderBy: o => o.OrderBy(x => x.Room)
                );
            return result;
        }

        private Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.ApartmentId);

            return sortBy.ToLower() switch
            {
                "room" => q => q.OrderBy(p => p.Room),
                "room_desc" => q => q.OrderByDescending(p => p.Room),
                _ => q => q.OrderByDescending(p => p.Room) // Default sort
            };
        }

        private async Task LoadUserProfileImagesAsync(ApartmentDto apartment)
        {
            var userIds = apartment.Users?.Select(ua => ua.User.UserId).ToList() ?? new List<int>();

            if (!userIds.Any())
                return;

            var profileImages = await _unitOfWork.GetRepository<Media>()
                .GetListAsync(
                    predicate: m => userIds.Contains(m.EntityId)
                        && m.Entity == nameof(User)
                        && m.Status == ActiveStatus.Active
                );

            var imageDict = profileImages.ToDictionary(m => m.EntityId, m => m.FilePath);

            foreach (var userApartment in apartment.Users)
            {
                if (userApartment.User != null && imageDict.TryGetValue(userApartment.User.UserId, out var imagePath))
                {
                    userApartment.User.ProfileImageUrl = imagePath;
                }
            }
        }

        public async Task<ApartmentDto> UpadteUserDataForAptAsync(int AptId, UpdateApartmentWithResidentDataDto dto)
        {
            var aptRepo = _unitOfWork.GetRepository<Apartment>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();
            var floorRepo = _unitOfWork.GetRepository<Floor>();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var apartment = await aptRepo.SingleOrDefaultAsync(
                    predicate: a => a.ApartmentId == AptId,
                    include: inc => inc.Include(a => a.UserApartments)
                                      .ThenInclude(ua => ua.User)
                );
                var floor = await floorRepo.SingleOrDefaultAsync(
                    predicate: f => f.FloorId == dto.FloorId
                );
                if (apartment == null)
                {
                    throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (floor == null)
                {
                    throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);
                }


                _mapper.Map(dto, apartment);
                aptRepo.UpdateAsync(apartment);

                var existingUserIds = apartment.UserApartments.Select(ua => ua.UserId).ToHashSet();
                var newUserIds = dto.Residents?.Select(r => r.UserId).ToHashSet() ?? new HashSet<int>();

                foreach (var userApartment in apartment.UserApartments.ToList())
                {
                    if (!newUserIds.Contains(userApartment.UserId))
                    {
                        userApartment.Status = ActiveStatus.Inactive;
                        userApartment.DisableAt = DateTime.Now;
                        uaRepo.UpdateAsync(userApartment);
                    }
                }

                if (dto.Residents != null)
                {
                    foreach (var residentDto in dto.Residents)
                    {
                        if (!existingUserIds.Contains(residentDto.UserId))
                        {
                            var newUserApartment = _mapper.Map<UserApartment>(residentDto);
                            await uaRepo.InsertAsync(newUserApartment);
                        }
                    }
                }

                await _unitOfWork.CommitAsync();
                var updatedApartmentDto = _mapper.Map<ApartmentDto>(apartment);
                await LoadUserProfileImagesAsync(updatedApartmentDto);
                return updatedApartmentDto;

            }
            catch
            (Exception ex)
            {
                throw new Exception("Cập nhật dữ liệu cư dân cho căn hộ thất bại: " + ex.Message);
            }
        }
    }
}
