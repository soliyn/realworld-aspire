using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleHandlers
{
    public static async Task<IResult> GetArticle(string slug, RealWorldDbContext dbContext)
    {
        var article = await dbContext.Articles
            .Include(x => x.Author)
            .Select(x => new GetArticleResponse
            {
                Slug = x.Slug,
                Title = x.Title,
                Description = x.Description,
                Body = x.Body,
                TagList = x.TagList.ToList(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Favorited = x.Favorited,
                FavoritesCount = x.FavoritesCount,
                Author = new GetArticleResponse.AuthorDto
                {
                    Username = x.Author.Username,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = x.Author.Following,
                }
            })
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(article);
    }

    public static async Task<IResult> GetArticles([AsParameters] GetArticlesRequest request, RealWorldDbContext dbContext)
    {
        const int defaultLimit = 20;
        int offset = request.Offset ?? 0;
        int limit = request.Limit ?? defaultLimit;

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
                Favorited = x.Favorited,
                FavoritesCount = x.FavoritesCount,
                Author = new GetArticlesResponse.Article.AuthorDto
                {
                    Username = x.Author.Username,
                    Bio = x.Author.Bio,
                    Image = x.Author.Image,
                    Following = x.Author.Following,
                }
            })
            .ToListAsync();

        return TypedResults.Ok(new GetArticlesResponse { Articles = articles, ArticlesCount =  articles.Count });
    }
    
}