using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;

namespace RealWorldAspire.ApiService.Features.Articles;

public static class ArticleHandlers
{
    public static async Task<IResult> GetArticle(string slug, RealWorldDbContext dbContext)
    {
        var article = await dbContext.Articles
            .Include(x => x.Author)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (article == null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetArticleResponse
        {
            Slug = article.Slug,
            Title = article.Title,
            Description = article.Description,
            Body = article.Body,
            TagList = article.TagList?.ToList() ?? [],
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Favorited = false, // Set based on user context if available
            FavoritesCount = article.FavoritesCount,
            Author = new GetArticleResponse.AuthorDto
            {
                Username = article.Author.Username,
                Bio = article.Author.Bio,
                Image = article.Author.Image,
                Following = false // Set based on user context if available
            }
        };
        return TypedResults.Ok(response);
    }
}