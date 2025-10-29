using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> GetComments(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserAsync(principal);

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        var comments = await dbContext.Comments
            .Where(c => c.Article!.Slug == slug)
            .Select(c => new GetCommentsResponse.CommentModel
            {
                Id = c.Id,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Body = c.Body,
                Author = new GetCommentsResponse.CommentModel.AuthorDto
                {
                    Username = c.Author.UserName,
                    Bio = c.Author.Bio,
                    Image = c.Author.Image,
                    Following = user != null && c.Author.Followers.Any(uf => uf.FollowerId == user.Id),
                },
            })
            .ToListAsync();

        return TypedResults.Ok(new GetCommentsResponse { Comments = comments });
    }
}