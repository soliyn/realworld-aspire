using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public static partial class ArticleHandlers
{
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
}