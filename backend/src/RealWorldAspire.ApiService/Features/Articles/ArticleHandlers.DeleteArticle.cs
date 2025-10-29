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
        RealWorldDbContext dbContext
    )
    {
        var user = await userManager.GetUserOrThrow(principal);

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        return article switch
        {
            null => TypedResults.NotFound(),
            _ when article.Author.Id != user.Id => TypedResults.Forbid(),
            _ => await Delete(),
        };

        async Task<IResult> Delete()
        {
            dbContext.Articles.Remove(article);
            await dbContext.SaveChangesAsync();

            return TypedResults.NoContent();
        }
    }
}