using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Exceptions;
using AptCare.Service.Extensions;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Services.Implements
{
    public sealed class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _http;
        public UserContext(IHttpContextAccessor http) => _http = http;

        public int CurrentUserId => _http.HttpContext?.User.GetUserId()
            ?? throw new AppValidationException("Unauthenticated.");

        public string Role => _http.HttpContext?.User.GetRole()
            ?? throw new AppValidationException("Unauthenticated.");

        public bool IsResident => string.Equals(Role, nameof(AccountRole.Resident), StringComparison.OrdinalIgnoreCase);
        public bool IsTechnicianLead => string.Equals(Role, nameof(AccountRole.TechnicianLead), StringComparison.OrdinalIgnoreCase);
        public bool IsTechnician => string.Equals(Role, nameof(AccountRole.Technician), StringComparison.OrdinalIgnoreCase);
        public bool IsManager => string.Equals(Role, nameof(AccountRole.Manager), StringComparison.OrdinalIgnoreCase);
        public bool IsReceptionist => string.Equals(Role, nameof(AccountRole.Receptionist), StringComparison.OrdinalIgnoreCase);

    }
}
