namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var articlesEndPoints = endpoints.MapGroup("/articles");

        articlesEndPoints.MapGet("/feed", ArticleHandlers.GetFeed);
        articlesEndPoints.MapGet("/{slug}", ArticleHandlers.GetArticle).AllowAnonymous();
        articlesEndPoints.MapDelete("/{slug}", ArticleHandlers.DeleteArticle);
        articlesEndPoints.MapGet("", ArticleHandlers.GetArticles).AllowAnonymous();
        articlesEndPoints.MapPost("", ArticleHandlers.CreateArticle);
        articlesEndPoints.MapPut("/{slug}", ArticleHandlers.UpdateArticle);

        articlesEndPoints.MapPost("{slug}/favorite", ArticleHandlers.FavoriteArticle);
        articlesEndPoints.MapDelete("{slug}/favorite", ArticleHandlers.UnfavoriteArticle);

        articlesEndPoints.MapPost("{slug}/comments", ArticleHandlers.CreateComment);
        articlesEndPoints.MapGet("{slug}/comments", ArticleHandlers.GetComments).AllowAnonymous();
        articlesEndPoints.MapDelete("{slug}/comments/{id}", ArticleHandlers.DeleteComment);

        return endpoints;
    }
}
