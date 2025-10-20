namespace RealWorldAspire.ApiService.Features.Users;

public class UserResponse
{
    public required UserModel User { get; set; }
    
    public class UserModel
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; }
        public string? Bio { get; set; }
        public string? Image { get; set; }
    }
}