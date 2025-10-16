namespace RealWorldAspire.ApiService.Data.Models;

public class UserFollow
{
    public int Id { get; set; }
    public string FollowerId { get; set; }
    public string FollowingId { get; set; }

    public AppUser Follower { get; set; }
    public AppUser Following { get; set; }
}