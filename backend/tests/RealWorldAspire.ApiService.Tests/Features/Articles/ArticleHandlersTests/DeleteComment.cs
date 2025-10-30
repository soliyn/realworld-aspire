using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class DeleteComment : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private AppUser _author = null!;
    private AppUser _commenter = null!;
    private AppUser _otherUser = null!;
    private ClaimsPrincipal _authorPrincipal = null!;
    private ClaimsPrincipal _commenterPrincipal = null!;
    private ClaimsPrincipal _otherUserPrincipal = null!;
    private Article _article = null!;
    private Comment _comment1 = null!;
    private Comment _comment2 = null!;
    private Comment _comment3 = null!;

    public DeleteComment(PostgresTestFixture fixture)
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
            Bio = "I write articles",
            Image = "https://example.com/author.jpg",
        };
        await context.UserManager.CreateAsync(_author);
        _author = (await context.UserManager.FindByEmailAsync(_author.Email))!;
        _authorPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_author);

        // Create commenter (who will own comments)
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

        // Create other user (not the comment author)
        _otherUser = new AppUser
        {
            Email = "other@example.com",
            UserName = "otheruser",
            Bio = "Just another user",
            Image = "https://example.com/other.jpg",
        };
        await context.UserManager.CreateAsync(_otherUser);
        _otherUser = (await context.UserManager.FindByEmailAsync(_otherUser.Email))!;
        _otherUserPrincipal = await context.SignInManager.CreateUserPrincipalAsync(_otherUser);

        // Create an article
        var articleCreatedAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        _article = new Article
        {
            Slug = "test-article",
            Title = "Test Article",
            Description = "An article with comments",
            Body = "Article body content",
            CreatedAt = articleCreatedAt,
            UpdatedAt = articleCreatedAt,
            Author = _author,
            Tags = [],
            FavoritedByUsers = [],
        };

        await context.DbContext.Articles.AddAsync(_article);
        await context.DbContext.SaveChangesAsync();

        // Add comments to the article
        var comment1Time = new DateTime(2025, 10, 28, 10, 0, 0, DateTimeKind.Utc);
        var comment2Time = new DateTime(2025, 10, 28, 11, 0, 0, DateTimeKind.Utc);
        var comment3Time = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        _comment1 = new Comment
        {
            Body = "First comment by commenter.",
            CreatedAt = comment1Time,
            UpdatedAt = comment1Time,
            Author = _commenter,
            Article = _article,
        };

        _comment2 = new Comment
        {
            Body = "Second comment by author.",
            CreatedAt = comment2Time,
            UpdatedAt = comment2Time,
            Author = _author,
            Article = _article,
        };

        _comment3 = new Comment
        {
            Body = "Third comment by commenter.",
            CreatedAt = comment3Time,
            UpdatedAt = comment3Time,
            Author = _commenter,
            Article = _article,
        };

        await context.DbContext.Comments.AddRangeAsync(_comment1, _comment2, _comment3);
        await context.DbContext.SaveChangesAsync();

        // Reload to get IDs
        _comment1 = await context.DbContext.Comments.FirstAsync(c => c.Body == "First comment by commenter.");
        _comment2 = await context.DbContext.Comments.FirstAsync(c => c.Body == "Second comment by author.");
        _comment3 = await context.DbContext.Comments.FirstAsync(c => c.Body == "Third comment by commenter.");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Delete_Comment_When_User_Is_Comment_Author()
    {
        // Arrange
        var commentId = _comment1.Id;

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentId,
                _commenterPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify comment was actually deleted from database
        using var context = ScopedContext.Create(_connectionString);
        var deletedComment = await context.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        deletedComment.ShouldBeNull();

        // Verify other comments still exist
        var remainingComments = await context.DbContext.Comments
            .Where(c => c.Article!.Slug == "test-article")
            .ToListAsync();

        remainingComments.Count.ShouldBe(2);
        remainingComments.Select(c => c.Body).ShouldContain("Second comment by author.");
        remainingComments.Select(c => c.Body).ShouldContain("Third comment by commenter.");
    }

    [Fact]
    public async Task Should_Not_Delete_Comment_When_User_Is_Not_Comment_Author()
    {
        // Arrange
        var commentId = _comment1.Id;

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentId,
                _otherUserPrincipal, // Different user trying to delete
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<ForbidHttpResult>();

        // Verify comment was NOT deleted from database
        using var context = ScopedContext.Create(_connectionString);
        var comment = await context.DbContext.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldNotBeNull();
        new { comment.Body, comment.Author.UserName }
            .ShouldBeEquivalentTo(new
            {
                Body = "First comment by commenter.",
                UserName = "commenteruser",
            });
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Article_Does_Not_Exist()
    {
        // Arrange
        var commentId = _comment1.Id;

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "non-existent-article",
                commentId,
                _commenterPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();

        // Verify comment still exists (was not deleted)
        using var context = ScopedContext.Create(_connectionString);
        var comment = await context.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Comment_Does_Not_Exist()
    {
        // Arrange
        var nonExistentCommentId = 999999;

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                nonExistentCommentId,
                _commenterPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Comment_Belongs_To_Different_Article()
    {
        // Arrange - Create another article with a comment
        using (var setupContext = ScopedContext.Create(_connectionString))
        {
            var author = await setupContext.UserManager.FindByEmailAsync(_author.Email);
            var commenter = await setupContext.UserManager.FindByEmailAsync(_commenter.Email);

            author.ShouldNotBeNull();
            commenter.ShouldNotBeNull();

            var articleCreatedAt = new DateTime(2025, 10, 22, 10, 0, 0, DateTimeKind.Utc);
            var otherArticle = new Article
            {
                Slug = "other-article",
                Title = "Other Article",
                Description = "Another article",
                Body = "Other article body",
                CreatedAt = articleCreatedAt,
                UpdatedAt = articleCreatedAt,
                Author = author,
                Tags = [],
                FavoritedByUsers = [],
            };

            await setupContext.DbContext.Articles.AddAsync(otherArticle);
            await setupContext.DbContext.SaveChangesAsync();

            var commentTime = new DateTime(2025, 10, 28, 14, 0, 0, DateTimeKind.Utc);
            var commentOnOtherArticle = new Comment
            {
                Body = "Comment on other article.",
                CreatedAt = commentTime,
                UpdatedAt = commentTime,
                Author = commenter,
                Article = otherArticle,
            };

            await setupContext.DbContext.Comments.AddAsync(commentOnOtherArticle);
            await setupContext.DbContext.SaveChangesAsync();
        }

        // Get the comment ID from the other article
        int commentId;
        using (var getContext = ScopedContext.Create(_connectionString))
        {
            var otherComment = await getContext.DbContext.Comments
                .Include(c => c.Article)
                .FirstOrDefaultAsync(c => c.Body == "Comment on other article.");
            otherComment.ShouldNotBeNull();
            commentId = otherComment.Id;
        }

        // Act - Try to delete comment from other article using wrong article slug
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article", // Wrong article slug
                commentId,
                _commenterPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();

        // Verify comment still exists
        using var verifyContext = ScopedContext.Create(_connectionString);
        var comment = await verifyContext.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Allow_Article_Author_To_Delete_Any_Comment_On_Their_Article()
    {
        // Arrange - Article author trying to delete commenter's comment
        var commentId = _comment1.Id; // Comment by commenter

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentId,
                _authorPrincipal, // Article author, but not comment author
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<ForbidHttpResult>();

        // Verify comment was NOT deleted
        using var context = ScopedContext.Create(_connectionString);
        var comment = await context.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Allow_User_To_Delete_Their_Own_Comment()
    {
        // Arrange - Commenter deleting their own comment
        var commentId = _comment3.Id; // Comment by commenter

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentId,
                _commenterPrincipal, // Comment author
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify comment was deleted
        using var context = ScopedContext.Create(_connectionString);
        var comment = await context.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Allow_Article_Author_To_Delete_Own_Comment_On_Own_Article()
    {
        // Arrange - Article author deleting their own comment on their article
        var commentId = _comment2.Id; // Comment by article author

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentId,
                _authorPrincipal, // Both article and comment author
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify comment was deleted
        using var context = ScopedContext.Create(_connectionString);
        var comment = await context.DbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        comment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Only_Delete_Specified_Comment_Not_Others()
    {
        // Arrange
        var commentToDeleteId = _comment2.Id;
        var initialCommentCount = 3;

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.DeleteComment(
                "test-article",
                commentToDeleteId,
                _authorPrincipal,
                userManager,
                dbContext)
        );

        // Assert
        result.ShouldBeOfType<NoContent>();

        // Verify only the specified comment was deleted
        using var context = ScopedContext.Create(_connectionString);
        var remainingComments = await context.DbContext.Comments
            .Where(c => c.Article!.Slug == "test-article")
            .ToListAsync();

        remainingComments.Count.ShouldBe(initialCommentCount - 1);
        remainingComments.ShouldNotContain(c => c.Id == commentToDeleteId);
        remainingComments.ShouldContain(c => c.Id == _comment1.Id);
        remainingComments.ShouldContain(c => c.Id == _comment3.Id);
    }
}