using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> UpdateArticle(
        string slug,
        CreateArticleRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserOrThrow(principal);

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        return article switch
        {
            null => TypedResults.NotFound(),
            _ when article.Author.Id != user.Id => TypedResults.Forbid(),
            _ => await Update(),
        };

        async Task<IResult> Update()
        {
            await UpdateAndSaveArticle();

            return TypedResults.Ok(
                new GetArticleResponse { Article = await CreateArticleResponse(dbContext, user, slug, cancellationToken) }
            );
        }

        async Task UpdateAndSaveArticle()
        {
            article.Title = request.Article.Title;
            article.Description = request.Article.Description;
            article.Body = request.Article.Body;
            article.Tags = await GetOrCreateTags(request.Article.TagList, dbContext, cancellationToken);
            article.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
