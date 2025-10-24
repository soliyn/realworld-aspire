using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Extensions;
using Slugify;

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
            query = query.Where(x => x.Tags.Any(t => t.Name == request.Tag));
        }

        if (request.Author != null)
        {
            query = query.Where(x => x.Author.UserName == request.Author);
        }

        if (request.Favorited != null)
        {
            query = query.Where(x => x.FavoritedByUsers.Any(u => u.UserName == request.Favorited));
        }

        var articles = await query
            .Include(x => x.Author)
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
                }
            })
            .ToListAsync();

        return TypedResults.Ok(new GetArticlesResponse { Articles = articles, ArticlesCount = articles.Count });
    }

    public static async Task<IResult> CreateArticle(
        CreateArticleRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserOrThrow(principal);

        var tags = await GetOrCreateTags(request.Article.TagList, dbContext);

        var now = DateTime.UtcNow;
        var article = new Article
        {
            Slug = new SlugHelper().GenerateSlug(request.Article.Title),
            Title = request.Article.Title,
            Description = request.Article.Description,
            Body = request.Article.Body,
            Tags = tags,
            CreatedAt = now,
            UpdatedAt = now,
            Author = user,
        };

        await dbContext.Articles.AddAsync(article);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(
            new GetArticleResponse { Article = await CreateArticleResponse(dbContext, user, new SlugHelper().GenerateSlug(request.Article.Title)) }
        );
    }

    public static async Task<IResult> UpdateArticle(
        string slug,
        CreateArticleRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext)
    {
        var user = await userManager.GetUserOrThrow(principal);

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(x => x.Slug == slug);

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
                new GetArticleResponse { Article = await CreateArticleResponse(dbContext, user, slug) }
            );
        }

        async Task UpdateAndSaveArticle()
        {
            article.Title = request.Article.Title;
            article.Description = request.Article.Description;
            article.Body = request.Article.Body;
            article.Tags = await GetOrCreateTags(request.Article.TagList, dbContext);
            article.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }
    }
    
    private static async Task<GetArticleResponse.ArticleModel> CreateArticleResponse(
        RealWorldDbContext dbContext, 
        AppUser user,
        string slug
    ) =>
        await dbContext.Articles
            .Where(x => x.Slug == slug)
            .Select(x => new GetArticleResponse.ArticleModel
            {
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                Body = x.Body,
                TagList = x.Tags.Select(t => t.Name).ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = x.FavoritedByUsers.Any(u => u.Id == user.Id),
                FavoritesCount = x.FavoritedByUsers.Count,
                Author = new GetArticleResponse.ArticleModel.AuthorDto
                {
                    Username = x.Author.UserName,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = x.Author.Followers.Any(uf => uf.FollowerId == user.Id),
                },
            })
            .FirstAsync();


    private static async Task<List<Tag>> GetOrCreateTags(List<string> tagNames, RealWorldDbContext dbContext)
    {
        var existingTags = await dbContext.Tags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync();

        var tags = new List<Tag>();
        foreach (var tagName in tagNames)
        {
            var existingTag = existingTags.FirstOrDefault(t => t.Name == tagName);

            if (existingTag != null)
            {
                tags.Add(existingTag);
            }
            else
            {
                var newTag = new Tag
                {
                    Name = tagName,
                    Articles = [],
                };
                tags.Add(newTag);
            }
        }

        return tags;
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
                TagList = x.Tags.Select(t => t.Name).ToList(),
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
                    Username = article.Author.UserName,
                    Bio = article.Author.Bio,
                    Image = article.Author.Image,
                    Following = true,
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
                TagList = x.Tags.Select(t => t.Name).ToList(),
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
                    Username = article.Author.UserName,
                    Bio = article.Author.Bio,
                    Image = article.Author.Image,
                    Following = false,
                }
            }
        });
    }
}