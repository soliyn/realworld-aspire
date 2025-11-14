using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> DeleteArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.GetUserOrThrow(principal, cancellationToken);

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        return article switch
        {
            null => TypedResults.NotFound(),
            _ when article.Author.Id != user.Id => TypedResults.Forbid(),
            _ => await Delete(),
        };

        async Task<IResult> Delete()
        {
            await DeleteArticleComments();

            dbContext.Articles.Remove(article);
            await dbContext.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }

        async Task DeleteArticleComments()
        {
            await dbContext.Comments
                .Where(c => c.Article!.ArticleId == article.ArticleId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}