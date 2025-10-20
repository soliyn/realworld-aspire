using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Data.Models;

public class Article
{
    public int ArticleId { get; set; }
    
    [MaxLength(ValidationConstants.Article.SlugMaxLength)]
    public required string Slug { get; set; }

    [MaxLength(ValidationConstants.Article.TitleMaxLength)]
    public required string Title { get; set; }
    
    [MaxLength(ValidationConstants.Article.DescriptionMaxLength)]
    public required string Description { get; set; }
    
    public required string Body { get; set; }
    
    public required List<string> TagList { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<AppUser> FavoritedByUsers { get; set; } = [];
    
    public required Author Author { get; set; }
}