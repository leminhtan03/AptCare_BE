using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
        Task<IPaginate<UserDto>> GetReSidentDataPageAsync(string searchQuery, string status, int page, int pageSize);
        Task<IPaginate<UserDto>> GetSystemUserPageAsync(string searchQuery, string role, string status, int page, int pageSize);
        Task<ImportResultDto> ImportResidentsFromExcelAsync(Stream fileStream);
        Task<UserDto> CreateAccountForNewUserAsync(CreateInforWithAccount createAccountDto);
        Task UpdateUserProfileImageAsync(UpdateUserImageProfileDto dto);
    }
}
