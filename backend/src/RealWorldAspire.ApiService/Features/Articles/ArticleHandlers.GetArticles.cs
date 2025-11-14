using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
    public static async Task<IResult> GetArticles(
        [AsParameters] GetArticlesRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        const int defaultLimit = 20;
        int offset = request.Offset ?? 0;
        int limit = request.Limit ?? defaultLimit;

        var user = await userManager.GetUserAsync(principal);

        IQueryable<Article> query = dbContext.Articles;

        query = query.WhereIf(request.Tag is not null,
            x => x.Tags.Any(t => t.Name == request.Tag)
        );
        query = query.WhereIf(request.Author is not null,
            x => x.Author.UserName == request.Author
        );
        query = query.WhereIf(request.Favorited is not null,
            x => x.FavoritedByUsers.Any(u => u.UserName == request.Favorited)
        );

        var totalCount = await query.CountAsync(cancellationToken);

        var articles = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(x => new GetArticlesResponse.Article()
            {
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                TagList = x.Tags.Select(t => t.Name).ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = user != null && x.FavoritedByUsers.Any(u => u.Id == user.Id),
                FavoritesCount = x.FavoritedByUsers.Count,
                Author = new GetArticlesResponse.Article.AuthorDto
                {
                    Username = x.Author.UserName,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = user != null && x.Author.Followers.Any(u => u.FollowerId == user.Id),
                },
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new GetArticlesResponse { Articles = articles, ArticlesCount = totalCount });
    }
}
