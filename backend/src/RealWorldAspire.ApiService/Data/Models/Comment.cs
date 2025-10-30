namespace RealWorldAspire.ApiService.Data.Models;

public class Comment
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Body { get; set; }

    public required AppUser Author { get; set; }
    public Article? Article { get; set; }
}