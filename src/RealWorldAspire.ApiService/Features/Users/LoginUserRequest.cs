namespace RealWorldAspire.ApiService.Features.Users;

public class LoginUserRequest
{
    public UserModel User { get; set; }
    public class UserModel
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}