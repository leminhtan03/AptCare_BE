using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IUserService
    {
        Task<Paginate<UserDto>> GetPageUsersAsync();
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto?> UpdateUserAsync(UpdateUserDto updateUserDto);
        //Task<Paginate<UserDto>> GetPageUsersAsync();
        //Task<bool> DeleteUserAsync(int userId);
    }
}
