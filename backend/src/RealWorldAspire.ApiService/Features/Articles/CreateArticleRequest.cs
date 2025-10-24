namespace RealWorldAspire.ApiService.Features.Articles;

public class CreateArticleRequest
{
    public required ArticleModel Article { get; set; }

    public class ArticleModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Body { get; set; }
        public List<string> TagList { get; set; } = [];
    }
}