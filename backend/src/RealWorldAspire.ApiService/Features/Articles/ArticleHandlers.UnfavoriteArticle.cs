using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> UnfavoriteArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.GetUserAsync(principal, cancellationToken);
        if (user == null)
        {
            return TypedResults.Unauthorized();
        }

        var article = await dbContext.Articles
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                ArticleId = x.ArticleId,
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                Body = x.Body,
                TagList = x.Tags.Select(t => t.Name).ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FavoritedCount = x.FavoritedByUsers.Count,
                IsFavorited = x.FavoritedByUsers.Any(u => u.Id == user.Id),
                Author = x.Author
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        if (article.IsFavorited)
        {
            var fa = new FavoriteArticle()
            {
                FavoritedByUsersId = user.Id,
                ArticleId = article.ArticleId,
            };
            dbContext.FavoriteArticles.Remove(fa);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok(new GetArticleResponse
        {
            Article = new GetArticleResponse.ArticleModel()
            {
                Slug = article.Slug,
                Title = article.Title,
                Description = article.Description,
                Body = article.Body,
                TagList = article.TagList.ToList(),
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt,
                Favorited = false,
                FavoritesCount = article.FavoritedCount - (article.IsFavorited ? 1 : 0),
                Author = new GetArticleResponse.ArticleModel.AuthorDto
                {
                    Username = article.Author.UserName,
                    Bio = article.Author.Bio,
                    Image = article.Author.Image,
                    Following = false,
                }
            }
        });
    }
}
