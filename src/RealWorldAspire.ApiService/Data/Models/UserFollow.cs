using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Data.Models;

public class UserFollow
{
    public int Id { get; set; }
    public required string FollowerId { get; set; }
    public required string FollowingId { get; set; }

    [Required]
    public AppUser? Follower { get; set; }

    [Required]
    public AppUser? Following { get; set; }
}