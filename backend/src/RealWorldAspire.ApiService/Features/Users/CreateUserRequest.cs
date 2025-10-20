namespace RealWorldAspire.ApiService.Features.Users;

public class CreateUserRequest
{
    public required UserModel User { get; set; }
    
    public class UserModel
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}