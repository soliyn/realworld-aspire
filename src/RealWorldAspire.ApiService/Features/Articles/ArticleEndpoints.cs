namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var articlesEndPoints = endpoints.MapGroup("/articles");

        articlesEndPoints.MapGet("/{slug}", ArticleHandlers.GetArticle);
        articlesEndPoints.MapGet("", ArticleHandlers.GetArticles);
        articlesEndPoints.MapPost("{slug}/favorite", ArticleHandlers.FavoriteArticle);
        articlesEndPoints.MapDelete("{slug}/favorite", ArticleHandlers.UnfavoriteArticle);
        
        return endpoints;
    }
}
