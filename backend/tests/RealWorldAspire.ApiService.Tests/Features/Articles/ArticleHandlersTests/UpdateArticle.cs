using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class UpdateArticle : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private ClaimsPrincipal _authorPrincipal = null!;
    private ClaimsPrincipal _otherUserPrincipal = null!;
    private AppUser _author = null!;
    private AppUser _otherUser = null!;
    private Article _existingArticle = null!;

    public UpdateArticle(PostgresTestFixture fixture)
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
            Bio = "Original author",
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
            Slug = "original-article-title",
            Title = "Original Article Title",
            Description = "Original description",
            Body = "Original body content",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Author = _author,
            Tags = new List<Tag>
            {
                new() { Name = "original" },
                new() { Name = "test" },
            },
            FavoritedByUsers = [],
        };

        await context.DbContext.Articles.AddAsync(_existingArticle);
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Update_Article()
    {
        // Arrange
        var updateTime = new DateTime(2025, 10, 28, 14, 30, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(updateTime);

        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "Updated Article Title",
                Description = "Updated description",
                Body = "Updated body content with new information",
                TagList = ["updated", "newTag", "test"],
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.UpdateArticle(
                "original-article-title",
                request,
                _authorPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider,
                default)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse
            {
                Article = new GetArticleResponse.ArticleModel
                {
                    Slug = "original-article-title", // Slug should not change
                    Title = "Updated Article Title",
                    Description = "Updated description",
                    Body = "Updated body content with new information",
                    TagList = ["test", "updated", "newTag"], // Order matches database return order
                    CreatedAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc), // CreatedAt unchanged
                    UpdatedAt = updateTime, // UpdatedAt should be updated
                    Favorited = false,
                    FavoritesCount = 0,
                    Author = new GetArticleResponse.ArticleModel.AuthorDto
                    {
                        Username = "authoruser",
                        Bio = "Original author",
                        Image = "https://example.com/author.jpg",
                        Following = false,
                    },
                },
            });

        // Verify article was actually updated in database
        using var context = ScopedContext.Create(_connectionString);
        var savedArticle = await context.DbContext.Articles
            .Include(a => a.Tags)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == "original-article-title");

        savedArticle.ShouldNotBeNull();
        new
        {
            savedArticle.Title,
            savedArticle.Description,
            savedArticle.Body,
            savedArticle.CreatedAt,
            savedArticle.UpdatedAt,
            Tags = savedArticle.Tags.Select(t => t.Name).OrderBy(n => n).ToArray(),
        }.ShouldBeEquivalentTo(new
        {
            Title = "Updated Article Title",
            Description = "Updated description",
            Body = "Updated body content with new information",
            CreatedAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updateTime,
            Tags = new[] { "newTag", "test", "updated" },
        });
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Article_Does_Not_Exist()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "Updated Title",
                Description = "Updated description",
                Body = "Updated body",
                TagList = ["updated"],
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.UpdateArticle(
                "non-existent-slug",
                request,
                _authorPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider,
                default)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task Should_Return_Forbid_When_User_Is_Not_Author()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "Attempted Update",
                Description = "Trying to update someone else's article",
                Body = "This should not work",
                TagList = ["forbidden"],
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.UpdateArticle(
                "original-article-title",
                request,
                _otherUserPrincipal, // Using different user's principal
                userManager,
                dbContext,
                fakeTimeProvider,
                default)
        );

        // Assert
        result.ShouldBeOfType<ForbidHttpResult>();

        // Verify article was NOT updated in database
        using var context = ScopedContext.Create(_connectionString);
        var savedArticle = await context.DbContext.Articles
            .FirstOrDefaultAsync(a => a.Slug == "original-article-title");

        savedArticle.ShouldNotBeNull();
        new { savedArticle.Title, savedArticle.Description, savedArticle.Body }
            .ShouldBeEquivalentTo(new
            {
                Title = "Original Article Title", // Should remain unchanged
                Description = "Original description",
                Body = "Original body content",
            });
    }

    [Fact]
    public async Task Should_Update_Tags_When_Updating_Article()
    {
        // Arrange
        var updateTime = new DateTime(2025, 10, 28, 15, 0, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(updateTime);

        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "Updated Article Title",
                Description = "Testing tag updates",
                Body = "Body content",
                TagList = ["newtag1", "newtag2"], // Completely new tags
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.UpdateArticle(
                "original-article-title",
                request,
                _authorPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider,
                default)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>();

        // Verify tags were completely replaced
        using var context = ScopedContext.Create(_connectionString);
        var savedArticle = await context.DbContext.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Slug == "original-article-title");

        savedArticle.ShouldNotBeNull();
        savedArticle.Tags.Select(t => t.Name).OrderBy(n => n).ShouldBe(new[] { "newtag1", "newtag2" });
    }

    [Fact]
    public async Task Should_Clear_Tags_When_Empty_TagList_Provided()
    {
        // Arrange
        var updateTime = new DateTime(2025, 10, 28, 16, 0, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(updateTime);

        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "Updated Article Title",
                Description = "Testing empty tags",
                Body = "Body content",
                TagList = [], // Empty tag list
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.UpdateArticle(
                "original-article-title",
                request,
                _authorPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider,
                default)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>();

        // Verify tags were cleared
        using var context = ScopedContext.Create(_connectionString);
        var savedArticle = await context.DbContext.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Slug == "original-article-title");

        savedArticle.ShouldNotBeNull();
        savedArticle.Tags.ShouldBeEmpty();
    }
}
