using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;
using Slugify;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> CreateArticle(
        CreateArticleRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserOrThrow(principal, cancellationToken);

        var tags = await GetOrCreateTags(request.Article.TagList, dbContext, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var article = new Article
        {
            Slug = new SlugHelper().GenerateSlug(request.Article.Title),
            Title = request.Article.Title,
            Description = request.Article.Description,
            Body = request.Article.Body,
            Tags = tags,
            CreatedAt = now,
            UpdatedAt = now,
            Author = user,
        };

        await dbContext.Articles.AddAsync(article, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(
            new GetArticleResponse { Article = await CreateArticleResponse(dbContext, user, new SlugHelper().GenerateSlug(request.Article.Title), cancellationToken) }
        );
    }
}
