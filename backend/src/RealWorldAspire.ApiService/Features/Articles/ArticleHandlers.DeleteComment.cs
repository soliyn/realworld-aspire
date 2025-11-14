using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> DeleteComment(
        string slug,
        int id,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserOrThrow(principal, cancellationToken);

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        var comment = await dbContext.Comments
            .Include(c => c.Author)
            .Include(c => c.Article)
            .FirstOrDefaultAsync(c => c.Id == id && c.Article!.Slug == slug, cancellationToken);

        return comment switch
        {
            null => TypedResults.NotFound(),
            _ when comment.Author.Id != user.Id => TypedResults.Forbid(),
            _ => await Delete(),
        };

        async Task<IResult> Delete()
        {
            dbContext.Comments.Remove(comment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }
    }
}