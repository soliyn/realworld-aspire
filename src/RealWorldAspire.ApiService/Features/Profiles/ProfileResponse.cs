namespace RealWorldAspire.ApiService.Features.Profiles;

public class ProfileResponse
{
    public required ProfileModel Profile { get; set; }

    public class ProfileModel
    {
        public required string Username { get; set; }
        public string? Bio { get; set; }
        public string? Image { get; set; }
        public bool Following { get; set; }
    }
}