namespace RealWorldAspire.ApiService.Features.Articles;

public class GetArticleResponse
{
    public ArticleModel Article { get; set; }
    
    public class ArticleModel
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }
        public List<string> TagList { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Favorited { get; set; }
        public int FavoritesCount { get; set; }
        public AuthorDto Author { get; set; }
    
        public class AuthorDto
        {
            public string Username { get; set; }
            public string Bio { get; set; }
            public string Image { get; set; }
            public bool Following { get; set; }
        }
    }
}
