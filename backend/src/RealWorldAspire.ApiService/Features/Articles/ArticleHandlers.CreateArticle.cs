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
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserOrThrow(principal);

        var tags = await GetOrCreateTags(request.Article.TagList, dbContext);

        var now = DateTime.UtcNow;
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

        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(
            new GetArticleResponse { Article = await CreateArticleResponse(dbContext, user, new SlugHelper().GenerateSlug(request.Article.Title)) }
        );
    }
}
