using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> GetArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserAsync(principal);

        var articleModel = await dbContext.Articles
            .Include(x => x.Author)
            .Select(x => new GetArticleResponse.ArticleModel
            {
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                Body = x.Body,
                TagList = x.Tags.Select(t => t.Name).ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = user != null && x.FavoritedByUsers.Any(u => u.Id == user.Id),
                FavoritesCount = x.FavoritedByUsers.Count,
                Author = new GetArticleResponse.ArticleModel.AuthorDto
                {
                    Username = x.Author.UserName,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = user != null && x.Author.Followers.Any(uf => uf.FollowerId == user.Id),
                },
            })
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (articleModel == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new GetArticleResponse { Article = articleModel });
    }
}
