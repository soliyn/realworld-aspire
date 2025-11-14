using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class GetComments : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly ClaimsPrincipal _anonymousPrincipal;
    private AppUser _author = null!;
    private AppUser _commenter1 = null!;
    private AppUser _commenter2 = null!;
    private ClaimsPrincipal _commenter1Principal = null!;
    private Article _articleWithComments = null!;
    private Article _articleWithoutComments = null!;

    public GetComments(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
        _anonymousPrincipal = new ClaimsPrincipal();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);

        // Create article author
        _author = new AppUser
        {
            Email = "author@example.com",
            UserName = "articleauthor",
            Bio = "I write great articles",
            Image = "https://example.com/author.jpg",
        };
        await context.UserManager.CreateAsync(_author);
        _author = (await context.UserManager.FindByEmailAsync(_author.Email))!;

        // Create commenter 1
        _commenter1 = new AppUser
        {
            Email = "commenter1@example.com",
            UserName = "commenter1",
            Bio = "I love commenting",
            Image = "https://example.com/commenter1.jpg",
        };
        await context.UserManager.CreateAsync(_commenter1);
        _commenter1 = (await context.UserManager.FindByEmailAsync(_commenter1.Email))!;
        _commenter1Principal = await context.SignInManager.CreateUserPrincipalAsync(_commenter1);

        // Create commenter 2
        _commenter2 = new AppUser
        {
            Email = "commenter2@example.com",
            UserName = "commenter2",
            Bio = "Another commenter",
            Image = "https://example.com/commenter2.jpg",
        };
        await context.UserManager.CreateAsync(_commenter2);
        _commenter2 = (await context.UserManager.FindByEmailAsync(_commenter2.Email))!;

        // Create article with comments
        var articleCreatedAt = new DateTime(2025, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        _articleWithComments = new Article
        {
            Slug = "article-with-comments",
            Title = "Article With Comments",
            Description = "An article that has comments",
            Body = "This article has several comments.",
            CreatedAt = articleCreatedAt,
            UpdatedAt = articleCreatedAt,
            Author = _author,
            Tags = [],
            FavoritedByUsers = [],
        };

        // Create article without comments
        var article2CreatedAt = new DateTime(2025, 10, 21, 10, 0, 0, DateTimeKind.Utc);
        _articleWithoutComments = new Article
        {
            Slug = "article-without-comments",
            Title = "Article Without Comments",
            Description = "An article with no comments",
            Body = "No one has commented yet.",
            CreatedAt = article2CreatedAt,
            UpdatedAt = article2CreatedAt,
            Author = _author,
            Tags = [],
            FavoritedByUsers = [],
        };

        await context.DbContext.Articles.AddRangeAsync(_articleWithComments, _articleWithoutComments);
        await context.DbContext.SaveChangesAsync();

        // Add comments to the first article
        var comment1Time = new DateTime(2025, 10, 28, 10, 0, 0, DateTimeKind.Utc);
        var comment2Time = new DateTime(2025, 10, 28, 11, 0, 0, DateTimeKind.Utc);
        var comment3Time = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        var comments = new List<Comment>
        {
            new()
            {
                Body = "First comment on this article.",
                CreatedAt = comment1Time,
                UpdatedAt = comment1Time,
                Author = _commenter1,
                Article = _articleWithComments,
            },
            new()
            {
                Body = "Second comment from another user.",
                CreatedAt = comment2Time,
                UpdatedAt = comment2Time,
                Author = _commenter2,
                Article = _articleWithComments,
            },
            new()
            {
                Body = "Third comment, also from commenter1.",
                CreatedAt = comment3Time,
                UpdatedAt = comment3Time,
                Author = _commenter1,
                Article = _articleWithComments,
            },
        };

        await context.DbContext.Comments.AddRangeAsync(comments);
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Return_All_Comments_For_Article()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-with-comments",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comments.Count.ShouldBe(3);

        // Verify first comment
        var firstComment = okResult.Value.Comments[0];
        new
        {
            firstComment.Body,
            firstComment.CreatedAt,
            firstComment.UpdatedAt,
            firstComment.Author.Username,
            firstComment.Author.Bio,
            firstComment.Author.Image,
            firstComment.Author.Following,
        }.ShouldBeEquivalentTo(new
        {
            Body = "First comment on this article.",
            CreatedAt = new DateTime(2025, 10, 28, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 10, 28, 10, 0, 0, DateTimeKind.Utc),
            Username = "commenter1",
            Bio = "I love commenting",
            Image = "https://example.com/commenter1.jpg",
            Following = false,
        });

        // Verify second comment
        var secondComment = okResult.Value.Comments[1];
        new
        {
            secondComment.Body,
            secondComment.Author.Username,
        }.ShouldBeEquivalentTo(new
        {
            Body = "Second comment from another user.",
            Username = "commenter2",
        });

        // Verify third comment
        var thirdComment = okResult.Value.Comments[2];
        new
        {
            thirdComment.Body,
            thirdComment.Author.Username,
        }.ShouldBeEquivalentTo(new
        {
            Body = "Third comment, also from commenter1.",
            Username = "commenter1",
        });
    }

    [Fact]
    public async Task Should_Return_Empty_List_For_Article_With_No_Comments()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-without-comments",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Article_Does_Not_Exist()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "non-existent-article",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task Should_Show_Following_True_When_User_Follows_Comment_Author()
    {
        // Arrange - Create a follower relationship
        using (var context = ScopedContext.Create(_connectionString))
        {
            var followerUser = new AppUser
            {
                Email = "follower@example.com",
                UserName = "follower",
                Bio = "I follow people",
                Image = "https://example.com/follower.jpg",
            };
            await context.UserManager.CreateAsync(followerUser);
            followerUser = (await context.UserManager.FindByEmailAsync(followerUser.Email))!;

            var userFollow = new UserFollow
            {
                FollowerId = followerUser.Id,
                FollowingId = _commenter1.Id,
            };
            await context.DbContext.UserFollows.AddAsync(userFollow);
            await context.DbContext.SaveChangesAsync();

            var followerPrincipal = await context.SignInManager.CreateUserPrincipalAsync(followerUser);

            // Act
            var result = await ArticleHandlers.GetComments(
                "article-with-comments",
                followerPrincipal,
                context.UserManager,
                context.DbContext,
                default);

            // Assert
            var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
            okResult.Value.ShouldNotBeNull();

            // First comment is from commenter1 - should show Following = true
            okResult.Value.Comments[0].Author.Username.ShouldBe("commenter1");
            okResult.Value.Comments[0].Author.Following.ShouldBe(true);

            // Second comment is from commenter2 - should show Following = false
            okResult.Value.Comments[1].Author.Username.ShouldBe("commenter2");
            okResult.Value.Comments[1].Author.Following.ShouldBe(false);

            // Third comment is from commenter1 - should show Following = true
            okResult.Value.Comments[2].Author.Username.ShouldBe("commenter1");
            okResult.Value.Comments[2].Author.Following.ShouldBe(true);
        }
    }

    [Fact]
    public async Task Should_Show_Following_False_For_Anonymous_Users()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-with-comments",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();

        // All comments should show Following = false for anonymous users
        okResult.Value.Comments.ShouldAllBe(c => c.Author.Following == false);
    }

    [Fact]
    public async Task Should_Return_Comments_With_All_Author_Details()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-with-comments",
                _commenter1Principal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comments.Count.ShouldBe(3);

        // Verify each comment has complete author information
        foreach (var comment in okResult.Value.Comments)
        {
            comment.Author.Username.ShouldNotBeNullOrWhiteSpace();
            comment.Author.Bio.ShouldNotBeNullOrWhiteSpace();
            comment.Author.Image.ShouldNotBeNullOrWhiteSpace();
            comment.Author.Following.ShouldBeOneOf(true, false);
        }

        // Verify specific author details for first comment
        var firstComment = okResult.Value.Comments[0];
        firstComment.Author.Username.ShouldBe("commenter1");
        firstComment.Author.Bio.ShouldBe("I love commenting");
        firstComment.Author.Image.ShouldBe("https://example.com/commenter1.jpg");
    }

    [Fact]
    public async Task Should_Not_Return_Comments_From_Other_Articles()
    {
        // Arrange - Add a comment to the second article
        using (var setupContext = ScopedContext.Create(_connectionString))
        {
            var author = await setupContext.UserManager.FindByEmailAsync(_author.Email);
            var commenter = await setupContext.UserManager.FindByEmailAsync(_commenter1.Email);
            var secondArticle = await setupContext.DbContext.Articles
                .FirstOrDefaultAsync(a => a.Slug == "article-without-comments");

            author.ShouldNotBeNull();
            commenter.ShouldNotBeNull();
            secondArticle.ShouldNotBeNull();

            var commentTime = new DateTime(2025, 10, 28, 13, 0, 0, DateTimeKind.Utc);
            var commentOnSecondArticle = new Comment
            {
                Body = "Comment on the second article.",
                CreatedAt = commentTime,
                UpdatedAt = commentTime,
                Author = commenter,
                Article = secondArticle,
            };
            await setupContext.DbContext.Comments.AddAsync(commentOnSecondArticle);
            await setupContext.DbContext.SaveChangesAsync();
        }

        // Act - Get comments for first article
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-with-comments",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert - Should only return 3 comments from the first article
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comments.Count.ShouldBe(3);
        okResult.Value.Comments.ShouldAllBe(c => c.Body != "Comment on the second article.");
    }

    [Fact]
    public async Task Should_Include_Comment_Ids()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetComments(
                "article-with-comments",
                _anonymousPrincipal,
                userManager,
                dbContext,
                default)
        );

        // Assert
        var okResult = result.ShouldBeOfType<Ok<GetCommentsResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Comments.Count.ShouldBe(3);

        // Verify all comments have valid IDs
        foreach (var comment in okResult.Value.Comments)
        {
            comment.Id.ShouldBeGreaterThan(0);
        }

        // Verify IDs are unique
        var ids = okResult.Value.Comments.Select(c => c.Id).ToList();
        ids.Distinct().Count().ShouldBe(ids.Count);
    }
}