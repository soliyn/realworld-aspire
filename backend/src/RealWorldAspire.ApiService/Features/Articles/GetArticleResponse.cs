namespace RealWorldAspire.ApiService.Features.Articles;

public class GetArticleResponse
{
    public required ArticleModel Article { get; set; }
    
    public class ArticleModel
    {
        public required string Slug { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Body { get; set; }
        public required List<string> TagList { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Favorited { get; set; }
        public int FavoritesCount { get; set; }
        public required AuthorDto Author { get; set; }
    
        public class AuthorDto
        {
            public required string Username { get; set; }
            public string? Bio { get; set; }
            public string? Image { get; set; }
            public bool Following { get; set; }
        }
    }
}
