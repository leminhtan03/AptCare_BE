
namespace AptCare.Service.Services.Interfaces
{
    public interface IUserContext
    {
        int CurrentUserId { get; }
        string Role { get; }
        bool IsResident { get; }
        bool IsManager { get; }
        bool IsTechnician { get; }
        bool IsTechnicianLead { get; }
        bool IsReceptionist { get; }
        bool IsAdmin { get; }

    }

}
