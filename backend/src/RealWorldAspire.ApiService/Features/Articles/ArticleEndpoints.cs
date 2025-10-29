namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var articlesEndPoints = endpoints.MapGroup("/articles");

        articlesEndPoints.MapGet("/{slug}", ArticleHandlers.GetArticle);
        articlesEndPoints.MapDelete("/{slug}", ArticleHandlers.DeleteArticle);
        articlesEndPoints.MapGet("", ArticleHandlers.GetArticles);
        articlesEndPoints.MapPost("", ArticleHandlers.CreateArticle).RequireAuthorization();
        articlesEndPoints.MapPut("/{slug}", ArticleHandlers.UpdateArticle).RequireAuthorization();

        articlesEndPoints.MapPost("{slug}/favorite", ArticleHandlers.FavoriteArticle).RequireAuthorization();
        articlesEndPoints.MapDelete("{slug}/favorite", ArticleHandlers.UnfavoriteArticle).RequireAuthorization();

        articlesEndPoints.MapPost("{slug}/comments", ArticleHandlers.CreateComment).RequireAuthorization();
        
        return endpoints;
    }
}
