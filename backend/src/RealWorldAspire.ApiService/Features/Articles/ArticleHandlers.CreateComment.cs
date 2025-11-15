using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> CreateComment(
        string slug,
        CreateCommentRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.GetUserOrThrow(principal);

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (article is null)
        {
            return TypedResults.NotFound();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var comment = new Comment
        {
            Body = request.Comment.Body,
            CreatedAt = now,
            UpdatedAt = now,
            Author = user,
            Article = article,
        };

        await dbContext.Comments.AddAsync(comment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Comments
            .Where(c => c.Id == comment.Id)
            .Select(c => new GetCommentResponse.CommentModel
            {
                Id = c.Id,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Body = c.Body,
                Author = new GetCommentResponse.CommentModel.AuthorDto
                {
                    Username = c.Author.UserName,
                    Bio = c.Author.Bio,
                    Image = c.Author.Image,
                    Following = c.Author.Followers.Any(uf => uf.FollowerId == user.Id),
                },
            })
            .FirstAsync(cancellationToken);

        return TypedResults.Ok(new GetCommentResponse { Comment = response });
    }
}