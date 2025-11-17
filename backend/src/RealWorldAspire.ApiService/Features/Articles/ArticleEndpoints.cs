namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var articlesEndPoints = endpoints.MapGroup("/articles")
            .WithTags("Articles")
            .WithOpenApi()
        ;

        articlesEndPoints
            .MapGet("/feed", ArticleHandlers.GetFeed)
            .WithName("GetArticleFeed")
            .WithSummary("Get recent articles from followed users")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapGet("/{slug}", ArticleHandlers.GetArticle)
            .WithName("GetArticle")
            .WithSummary("Get an article by slug")
            .AllowAnonymous()
        ;

        articlesEndPoints
            .MapDelete("/{slug}", ArticleHandlers.DeleteArticle)
            .WithName("DeleteArticle")
            .WithSummary("Delete an article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapGet("", ArticleHandlers.GetArticles)
            .WithName("GetArticles")
            .WithSummary("Get articles with optional filters")
            .AllowAnonymous()
        ;

        articlesEndPoints
            .MapPost("", ArticleHandlers.CreateArticle)
            .WithName("CreateArticle")
            .WithSummary("Create a new article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapPut("/{slug}", ArticleHandlers.UpdateArticle)
            .WithName("UpdateArticle")
            .WithSummary("Update an existing article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapPost("{slug}/favorite", ArticleHandlers.FavoriteArticle)
            .WithName("FavoriteArticle")
            .WithSummary("Favorite an article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapDelete("{slug}/favorite", ArticleHandlers.UnfavoriteArticle)
            .WithName("UnfavoriteArticle")
            .WithSummary("Unfavorite an article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapPost("{slug}/comments", ArticleHandlers.CreateComment)
            .WithName("CreateComment")
            .WithSummary("Add a comment to an article")
            .RequireAuthorization()
        ;

        articlesEndPoints
            .MapGet("{slug}/comments", ArticleHandlers.GetComments)
            .WithName("GetComments")
            .WithSummary("Get comments for an article")
            .AllowAnonymous()
        ;

        articlesEndPoints
            .MapDelete("{slug}/comments/{id}", ArticleHandlers.DeleteComment)
            .WithName("DeleteComment")
            .WithSummary("Delete a comment from an article")
            .RequireAuthorization()
        ;

        return endpoints;
    }
}
