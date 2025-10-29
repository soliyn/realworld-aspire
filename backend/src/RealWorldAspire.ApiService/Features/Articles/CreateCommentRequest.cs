namespace RealWorldAspire.ApiService.Features.Articles;

public class CreateCommentRequest
{
    public required CommentModel Comment { get; set; }

    public class CommentModel
    {
        public required string Body { get; set; }
    }
}