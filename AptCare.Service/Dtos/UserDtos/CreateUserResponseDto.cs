
namespace AptCare.Service.Dtos.UserDtos
{
    public class CreateUserResponseDto
    {
        public UserDto User { get; set; } = null!;
        public bool AccountCreated { get; set; }
        public string? EmailSentMessage { get; set; }
        public string Message { get; set; } = null!;
    }
}
