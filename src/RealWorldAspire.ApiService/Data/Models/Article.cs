using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Data.Models;

public class Article
{
    public int ArticleId { get; set; }
    
    [MaxLength(50)]
    public required string Slug { get; set; }

    [MaxLength(50)]
    public required string Title { get; set; }
    
    [MaxLength(200)]
    public required string Description { get; set; }
    
    public required string Body { get; set; }
    
    public required List<string> TagList { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public bool Favorited { get; set; }
    
    public int FavoritesCount { get; set; }
    
    public required Author Author { get; set; }
}