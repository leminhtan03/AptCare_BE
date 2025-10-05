using AptCare.Repository;
using AptCare.Repository.Entities;
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
            if (await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.Email == createUserDto.Email) != null)
            {
                throw new InvalidOperationException("Email đã tồn tại.");
            }
            if (await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.PhoneNumber == createUserDto.PhoneNumber) != null)
            {
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");
            }
            var user = _mapper.Map<User>(createUserDto);
            await _unitOfWork.GetRepository<User>().InsertAsync(user);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var userExist = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == userId, include: u => u.Include(u => u.Account));
            if (userExist == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }
            if (userExist.Account != null)
            {
                throw new InvalidOperationException("Không thể xóa người dùng có tài khoản liên kết.");
            }
            if (userExist.Reports != null)
            {
                throw new InvalidOperationException("Không thể xóa người dùng đã có đơn hàng");
            }

            _unitOfWork.GetRepository<User>().DeleteAsync(userExist);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public Task<Paginate<UserDto>> GetPageUsersAsync()
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

        public async Task<UserDto?> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            var userExist = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == updateUserDto.UserId);
            if (userExist == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }
            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != userExist.Email)
            {
                if (await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.Email == updateUserDto.Email) != null)
                {
                    throw new InvalidOperationException("Email đã tồn tại.");
                }
            }
            if (!string.IsNullOrEmpty(updateUserDto.Phone) && updateUserDto.Phone != userExist.PhoneNumber)
            {
                if (await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.PhoneNumber == updateUserDto.Phone) != null)
                {
                    throw new InvalidOperationException("Số điện thoại đã tồn tại.");
                }
            }
            _mapper.Map(updateUserDto, userExist);
            if (!string.IsNullOrEmpty(updateUserDto.Status))
            {
                if (Enum.TryParse(updateUserDto.Status, true, out Repository.Enum.ActiveStatus newStatus))
                {
                    userExist.Status = newStatus;
                }
                else
                {
                    throw new ArgumentException($"Trạng thái '{updateUserDto.Status}' không hợp lệ.");
                }
            }
            _unitOfWork.GetRepository<User>().UpdateAsync(userExist);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<UserDto>(userExist);
        }

    }
}
