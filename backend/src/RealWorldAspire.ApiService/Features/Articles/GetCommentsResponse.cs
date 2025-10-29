namespace RealWorldAspire.ApiService.Features.Articles;

public class GetCommentsResponse
{
    public required List<CommentModel> Comments { get; set; }

    public class CommentModel
    {
        public required int Id { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
        public required string Body { get; set; }
        public required AuthorDto Author { get; set; }

        public class AuthorDto
        {
            public required string Username { get; set; }
            public string? Bio { get; set; }
            public string? Image { get; set; }
            public required bool Following { get; set; }
        }
    }
}
