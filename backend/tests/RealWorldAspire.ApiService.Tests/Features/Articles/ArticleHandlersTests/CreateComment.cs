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
public class CreateComment : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private ClaimsPrincipal _authorPrincipal = null!;
    private ClaimsPrincipal _commenterPrincipal = null!;
    private AppUser _author = null!;
    private AppUser _commenter = null!;
    private Article _existingArticle = null!;

    public CreateComment(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);

        // Create article author
        _author = new AppUser
        {
            Email = "author@example.com",
            UserName = "articleauthor",
            Bio = "Article author bio",
            Image = "https://example.com/author.jpg",
        };
        await context.UserManager.CreateAsync(_author);
        _author = (await context.UserManager.FindByEmailAsync(_author.Email))!;
        _authorPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_author);

        // Create commenter user
        _commenter = new AppUser
        {
            Email = "commenter@example.com",
            UserName = "commenteruser",
            Bio = "I love commenting",
            Image = "https://example.com/commenter.jpg",
        };
        await context.UserManager.CreateAsync(_commenter);
        _commenter = (await context.UserManager.FindByEmailAsync(_commenter.Email))!;
        _commenterPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_commenter);

        // Create an existing article
        var createdAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        _existingArticle = new Article
        {
            Slug = "test-article-slug",
            Title = "Test Article",
            Description = "Test article description",
            Body = "Test article body content",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Author = _author,
            Tags = new List<Tag>
            {
                new() { Name = "test" },
            },
            FavoritedByUsers = [],
        };

        await context.DbContext.Articles.AddAsync(_existingArticle);
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Create_Comment()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 10, 28, 14, 30, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        var request = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "This is a great article! Thanks for sharing.",
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "test-article-slug",
                request,
                _commenterPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comment.Id.ShouldBeGreaterThan(0);
        new
        {
            okResult.Value.Comment.CreatedAt,
            okResult.Value.Comment.UpdatedAt,
            okResult.Value.Comment.Body,
            okResult.Value.Comment.Author.Username,
            okResult.Value.Comment.Author.Bio,
            okResult.Value.Comment.Author.Image,
            okResult.Value.Comment.Author.Following,
        }.ShouldBeEquivalentTo(new
        {
            CreatedAt = fixedTime,
            UpdatedAt = fixedTime,
            Body = "This is a great article! Thanks for sharing.",
            Username = "commenteruser",
            Bio = "I love commenting",
            Image = "https://example.com/commenter.jpg",
            Following = false,
        });

        // Verify comment was actually saved to database
        using var context = ScopedContext.Create(_connectionString);
        var savedComment = await context.DbContext.Comments
            .Include(c => c.Author)
            .Include(c => c.Article)
            .FirstOrDefaultAsync(c => c.Body == "This is a great article! Thanks for sharing.");

        savedComment.ShouldNotBeNull();
        savedComment.Author.ShouldNotBeNull();
        savedComment.Article.ShouldNotBeNull();
        new
        {
            savedComment.Body,
            savedComment.CreatedAt,
            savedComment.UpdatedAt,
            AuthorUsername = savedComment.Author.UserName,
            ArticleSlug = savedComment.Article.Slug,
        }.ShouldBeEquivalentTo(new
        {
            Body = "This is a great article! Thanks for sharing.",
            CreatedAt = fixedTime,
            UpdatedAt = fixedTime,
            AuthorUsername = "commenteruser",
            ArticleSlug = "test-article-slug",
        });
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Article_Does_Not_Exist()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var request = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "This comment will not be created.",
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "non-existent-article-slug",
                request,
                _commenterPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();

        // Verify no comment was created
        using var context = ScopedContext.Create(_connectionString);
        var commentCount = await context.DbContext.Comments.CountAsync();
        commentCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Show_Following_False_When_User_Comments_On_Own()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 10, 28, 15, 0, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        var request = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "Great work!",
            },
        };

        // Act - Commenter creates a comment
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "test-article-slug",
                request,
                _commenterPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Assert - User doesn't follow themselves, so Following should be false
        var okResult = result.ShouldBeOfType<Ok<GetCommentResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comment.Author.Username.ShouldBe("commenteruser");
        okResult.Value.Comment.Author.Following.ShouldBe(false);
    }

    [Fact]
    public async Task Should_Allow_Author_To_Comment_On_Own_Article()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 10, 28, 16, 0, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        var request = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "I'm adding more context to my own article.",
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "test-article-slug",
                request,
                _authorPrincipal, // Author commenting on own article
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comment.Id.ShouldBeGreaterThan(0);
        new
        {
            okResult.Value.Comment.CreatedAt,
            okResult.Value.Comment.UpdatedAt,
            okResult.Value.Comment.Body,
            okResult.Value.Comment.Author.Username,
            okResult.Value.Comment.Author.Bio,
            okResult.Value.Comment.Author.Image,
            okResult.Value.Comment.Author.Following,
        }.ShouldBeEquivalentTo(new
        {
            CreatedAt = fixedTime,
            UpdatedAt = fixedTime,
            Body = "I'm adding more context to my own article.",
            Username = "articleauthor",
            Bio = "Article author bio",
            Image = "https://example.com/author.jpg",
            Following = false, // Author doesn't follow themselves
        });
    }

    [Fact]
    public async Task Should_Create_Multiple_Comments_On_Same_Article()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 10, 28, 17, 0, 0, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        var request1 = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "First comment",
            },
        };

        var request2 = new CreateCommentRequest
        {
            Comment = new CreateCommentRequest.CommentModel
            {
                Body = "Second comment",
            },
        };

        // Act - Create first comment
        var result1 = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "test-article-slug",
                request1,
                _commenterPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Act - Create second comment
        var result2 = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateComment(
                "test-article-slug",
                request2,
                _authorPrincipal,
                userManager,
                dbContext,
                fakeTimeProvider)
        );

        // Assert
        result1.ShouldBeOfType<Ok<GetCommentResponse>>();
        result2.ShouldBeOfType<Ok<GetCommentResponse>>();

        // Verify both comments were saved
        using var context = ScopedContext.Create(_connectionString);
        var comments = await context.DbContext.Comments
            .Include(c => c.Article)
            .Where(c => c.Article != null && c.Article.Slug == "test-article-slug")
            .ToListAsync();

        comments.Count.ShouldBe(2);
        comments.Select(c => c.Body).ShouldContain("First comment");
        comments.Select(c => c.Body).ShouldContain("Second comment");
    }
}
