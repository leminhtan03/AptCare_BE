using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
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
        Task<CreateUserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<string> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
        Task<IPaginate<UserGetAllDto>> GetProfileDataPageAsync(UserPaginateDto dto);
        Task<ImportResultDto> ImportResidentsFromExcelAsync(Stream fileStream);
        Task UpdateUserProfileImageAsync(UpdateUserImageProfileDto dto);
        Task<string> InactivateUserAsync(int userId, InactivateUserDto dto);
    }
}
