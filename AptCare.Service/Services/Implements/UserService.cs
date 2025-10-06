using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        public UserService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<UserService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            throw new NotImplementedException();


        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IPaginate<UserDto>> GetReSidentDataPageAsync(string searchQuery, string status, int page, int pageSize)
        {
            var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(
                selector: u => _mapper.Map<UserDto>(u),
                predicate: u =>
                    (string.IsNullOrEmpty(searchQuery) ||
                        ((u.FirstName + " " + u.LastName).ToLower().Contains(searchQuery) ||
                        u.Email.ToLower().Contains(searchQuery) ||
                        u.PhoneNumber.ToLower().Contains(searchQuery) ||
                        u.CitizenshipIdentity.ToLower().Contains(searchQuery))) &&
                        (u.Account == null || u.Account.Role == AccountRole.Resident) &&
                    (string.IsNullOrEmpty(status) || u.Status.ToString() == status),
                include: source => source
                    .Include(u => u.Account)
                    .Include(u => u.UserApartments)
                    .ThenInclude(ua => ua.Apartment),
                orderBy: users => users.OrderBy(u => u.UserId),
                page: page,
                size: pageSize
            );
            return users;
        }

        private Func<IQueryable<User>, IOrderedQueryable<User>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return null;

            return sortBy.ToLower() switch
            {
                "email" => q => q.OrderBy(p => p.Email),
                "birthday" => q => q.OrderBy(p => p.Birthday),
                "email_desc" => q => q.OrderByDescending(p => p.Email),
                "birthday_desc" => q => q.OrderByDescending(p => p.Birthday),
                "id" => q => q.OrderBy(p => p.UserId),
                "id_desc" => q => q.OrderByDescending(p => p.UserId),
                _ => q => q.OrderByDescending(p => p.UserId)
            };
        }

        public async Task<UserDto?> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            throw new NotImplementedException();
        }

    }
}
