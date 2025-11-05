using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<IPaginate<UserDto>> GetSystemUserPageAsync(string searchQuery, string role, string status, int page, int pageSize);
        Task<string> CreateAccountForUserAsync(int Id);
        Task<string> TogleAccontStatus(int accountId);

    }
}
