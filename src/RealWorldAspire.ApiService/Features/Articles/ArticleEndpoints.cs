namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var aep = endpoints.MapGroup("/articles");

        aep.MapGet("/{slug}", ArticleHandlers.GetArticle);
        
        return endpoints;
    }
}