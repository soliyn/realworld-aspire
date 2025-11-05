using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class DeleteArticle : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private ClaimsPrincipal _authorPrincipal = null!;
    private ClaimsPrincipal _otherUserPrincipal = null!;
    private AppUser _author = null!;
    private AppUser _otherUser = null!;
    private Article _existingArticle = null!;

    public DeleteArticle(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);

        // Create author user
        _author = new AppUser
        {
            Email = "author@example.com",
            UserName = "authoruser",
            Bio = "Article author",
            Image = "https://example.com/author.jpg",
        };
        await context.UserManager.CreateAsync(_author);
        _author = (await context.UserManager.FindByEmailAsync(_author.Email))!;
        _authorPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_author);

        // Create other user (not the author)
        _otherUser = new AppUser
        {
            Email = "other@example.com",
            UserName = "otheruser",
            Bio = "Not the author",
            Image = "https://example.com/other.jpg",
        };
        await context.UserManager.CreateAsync(_otherUser);
        _otherUser = (await context.UserManager.FindByEmailAsync(_otherUser.Email))!;
        _otherUserPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_otherUser);

        // Create an existing article
        var createdAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        _existingArticle = new Article
        {
            Slug = "article-to-delete",
            Title = "Article To Delete",
            Description = "This article will be deleted in tests",
            Body = "Article body content",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Author = _author,
            Tags = new List<Tag>
            {
                new() { Name = "test" },
                new() { Name = "delete" },
            },
            FavoritedByUsers = [],
        };

        await context.DbContext.Articles.AddAsync(_existingArticle);
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Delete_Article_When_User_Is_Author()
    {
        // Arrange
        var slug = "article-to-delete";

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteArticle(
                slug,
                _authorPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify article was actually deleted from database
        using var context = ScopedContext.Create(_connectionString);
        var deletedArticle = await context.DbContext.Articles
            .FirstOrDefaultAsync(a => a.Slug == slug);

        deletedArticle.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Delete_Article_When_User_Is_Not_Author()
    {
        // Arrange
        var slug = "article-to-delete";

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteArticle(
                slug,
                _otherUserPrincipal, // Using different user's principal
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<ForbidHttpResult>();

        // Verify article was NOT deleted from database
        using var context = ScopedContext.Create(_connectionString);
        var article = await context.DbContext.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Slug == slug);

        article.ShouldNotBeNull();
        new { article.Slug, article.Title, article.Description, article.Body }
            .ShouldBeEquivalentTo(new
            {
                Slug = "article-to-delete",
                Title = "Article To Delete",
                Description = "This article will be deleted in tests",
                Body = "Article body content",
            });
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Article_Does_Not_Exist()
    {
        // Arrange
        var nonExistentSlug = "non-existent-article";

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteArticle(
                nonExistentSlug,
                _authorPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task Should_Delete_Article_With_Comments()
    {
        // Arrange
        var slug = "article-to-delete";

        // Add comments to the article
        using (var context = ScopedContext.Create(_connectionString))
        {
            var article = await context.DbContext.Articles
                .FirstOrDefaultAsync(a => a.Slug == slug);
            article.ShouldNotBeNull();

            var commentAuthor = new AppUser
            {
                Email = "commenter@example.com",
                UserName = "commenteruser",
                Bio = "Comment author",
                Image = "https://example.com/commenter.jpg",
            };
            await context.UserManager.CreateAsync(commentAuthor);
            commentAuthor = (await context.UserManager.FindByEmailAsync(commentAuthor.Email))!;

            var createdAt = new DateTime(2025, 10, 21, 10, 0, 0, DateTimeKind.Utc);
            var comments = new List<Comment>
            {
                new()
                {
                    Body = "First comment on article",
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    Author = commentAuthor,
                    Article = article,
                },
                new()
                {
                    Body = "Second comment on article",
                    CreatedAt = createdAt.AddMinutes(5),
                    UpdatedAt = createdAt.AddMinutes(5),
                    Author = commentAuthor,
                    Article = article,
                },
            };

            await context.DbContext.Comments.AddRangeAsync(comments);
            await context.DbContext.SaveChangesAsync();
        }

        // Verify comments exist before deletion
        using (var context = ScopedContext.Create(_connectionString))
        {
            var commentsCount = await context.DbContext.Comments
                .CountAsync(c => c.Article!.Slug == slug);
            commentsCount.ShouldBe(2);
        }

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteArticle(
                slug,
                _authorPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify article was deleted
        using (var context = ScopedContext.Create(_connectionString))
        {
            var deletedArticle = await context.DbContext.Articles
                .FirstOrDefaultAsync(a => a.Slug == slug);
            deletedArticle.ShouldBeNull();

            // Verify comments were cascade deleted
            var commentsCount = await context.DbContext.Comments
                .CountAsync(c => c.Article!.Slug == slug);
            commentsCount.ShouldBe(0);
        }
    }
}
