using System.Security.Claims;

namespace AptCare.Service.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var val = user?.FindFirst("UserId")?.Value
                   ?? throw new UnauthorizedAccessException("Missing UserId claim.");
            if (!int.TryParse(val, out var id))
                throw new UnauthorizedAccessException("Invalid UserId claim.");
            return id;
        }

        public static string GetRole(this ClaimsPrincipal user)
            => user?.FindFirst(ClaimTypes.Role)?.Value
               ?? throw new UnauthorizedAccessException("Missing Role claim.");
    }
}
