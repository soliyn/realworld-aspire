using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Features.Articles;

public class CreateArticleRequest
{
    public required ArticleModel Article { get; set; }

    public class ArticleModel
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public required string Description { get; set; }

        [Required]
        [StringLength(50000, MinimumLength = 1)]
        public required string Body { get; set; }

        [MaxLength(20)]
        public required List<string> TagList { get; set; } = [];
    }
}