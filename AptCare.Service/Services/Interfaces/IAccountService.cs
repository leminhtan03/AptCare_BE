using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<string> CreateAccountForUserAsync(int Id);
    }
}
