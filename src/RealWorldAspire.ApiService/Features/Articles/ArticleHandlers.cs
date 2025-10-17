using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleHandlers
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
                TagList = x.TagList.ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = user != null && x.FavoritedByUsers.Any(u => u.Id == user.Id),
                FavoritesCount = x.FavoritedByUsers.Count,
                Author = new GetArticleResponse.ArticleModel.AuthorDto
                {
                    Username = x.Author.Username,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = x.Author.Following,
                }
            })
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (articleModel == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new GetArticleResponse { Article = articleModel });
    }

    public static async Task<IResult> GetArticles(
        [AsParameters] GetArticlesRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        const int defaultLimit = 20;
        int offset = request.Offset ?? 0;
        int limit = request.Limit ?? defaultLimit;

        var user = await userManager.GetUserAsync(principal);

        IQueryable<Article> query = dbContext.Articles
                .Include(x => x.Author)
            ;

        if (request.Tag != null)
        {
            query = query.Where(x => x.TagList.Contains(request.Tag));
        }

        if (request.Author != null)
        {
            query = query.Where(x => x.Author.Username == request.Author);
        }

        if (request.Favorited != null)
        {
            query = query.Where(x => x.FavoritedByUsers.Any(u => u.UserName == request.Favorited));
        }

        var articles = await query
            .Skip(offset)
            .Take(limit)
            .Select(x => new GetArticlesResponse.Article()
            {
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                TagList = x.TagList.ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = user != null && x.FavoritedByUsers.Any(u => u.Id == user.Id),
                FavoritesCount = x.FavoritedByUsers.Count,
                Author = new GetArticlesResponse.Article.AuthorDto
                {
                    Username = x.Author.Username,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = x.Author.Following,
                }
            })
            .ToListAsync();

        return TypedResults.Ok(new GetArticlesResponse { Articles = articles, ArticlesCount = articles.Count });
    }

    public static async Task<IResult> FavoriteArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserAsync(principal);
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
                TagList = x.TagList.ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FavoritedCount = x.FavoritedByUsers.Count,
                IsFavorited = x.FavoritedByUsers.Any(u => u.Id == user.Id),
                Author = x.Author
            })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        if (!article.IsFavorited)
        {
            var fa = new FavoriteArticle()
            {
                FavoritedByUsersId = user.Id,
                ArticleId = article.ArticleId,
            };
            await dbContext.FavoriteArticles.AddAsync(fa);
            await dbContext.SaveChangesAsync();
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
                Favorited = true,
                FavoritesCount = article.FavoritedCount + (article.IsFavorited ? 0 : 1),
                Author = new GetArticleResponse.ArticleModel.AuthorDto
                {
                    Username = article.Author.Username,
                    Bio = article.Author.Bio,
                    Image = article.Author.Image,
                    Following = article.Author.Following,
                }
            }
        });
    }

    public static async Task<IResult> UnfavoriteArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext
    )
    {
        var user = await userManager.GetUserAsync(principal);
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
                TagList = x.TagList.ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FavoritedCount = x.FavoritedByUsers.Count,
                IsFavorited = x.FavoritedByUsers.Any(u => u.Id == user.Id),
                Author = x.Author
            })
            .FirstOrDefaultAsync();

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
            await dbContext.SaveChangesAsync();
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
                    Username = article.Author.Username,
                    Bio = article.Author.Bio,
                    Image = article.Author.Image,
                    Following = article.Author.Following,
                }
            }
        });
    }
}