using System.ComponentModel.DataAnnotations;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public class CreateArticleRequest
{
    public required ArticleModel Article { get; set; }

    public class ArticleModel
    {
        [Required]
        [StringLength(ValidationConstants.Article.TitleMaxLength, MinimumLength = 1)]
        public required string Title { get; set; }

        [Required]
        [StringLength(ValidationConstants.Article.DescriptionMaxLength, MinimumLength = 1)]
        public required string Description { get; set; }

        [Required]
        public required string Body { get; set; }

        public required List<string> TagList { get; set; } = [];
    }
}