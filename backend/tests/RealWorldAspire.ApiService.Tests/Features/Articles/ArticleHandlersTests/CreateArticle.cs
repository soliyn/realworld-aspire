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
public class CreateArticle : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;
    private ClaimsPrincipal _principal = null!;
    private AppUser _author = null!;

    public CreateArticle(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);

        _author = new AppUser
        {
            Email = "author@example.com",
            UserName = "testauthor",
            Bio = "Test author bio",
            Image = "https://example.com/author.jpg",
        };

        await context.UserManager.CreateAsync(_author);
        _author = (await context.UserManager.FindByEmailAsync(_author.Email))!;

        _principal = await context.SignInManager.CreateUserPrincipalAsync(_author);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Create_Article()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 10, 28, 12, 30, 45, DateTimeKind.Utc);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);

        var request = new CreateArticleRequest
        {
            Article = new CreateArticleRequest.ArticleModel
            {
                Title = "How to Train Your Dragon",
                Description = "Ever wonder how?",
                Body = "You have to believe in yourself and your dragon.",
                TagList = ["dragons", "training", "fantasy"],
            },
        };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.CreateArticle(request, _principal, userManager, dbContext, fakeTimeProvider, default)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>();
        var okResult = (Ok<GetArticleResponse>)result;
        okResult.Value.ShouldNotBeNull();

        var articleResponse = okResult.Value.Article;
        articleResponse.Slug.ShouldBe("how-to-train-your-dragon");
        articleResponse.Title.ShouldBe("How to Train Your Dragon");
        articleResponse.Description.ShouldBe("Ever wonder how?");
        articleResponse.Body.ShouldBe("You have to believe in yourself and your dragon.");
        articleResponse.TagList.ShouldBe(new List<string> { "dragons", "training", "fantasy" });
        articleResponse.CreatedAt.ShouldBe(fixedTime);
        articleResponse.UpdatedAt.ShouldBe(fixedTime);
        articleResponse.Favorited.ShouldBe(false);
        articleResponse.FavoritesCount.ShouldBe(0);
        articleResponse.Author.Username.ShouldBe("testauthor");
        articleResponse.Author.Bio.ShouldBe("Test author bio");
        articleResponse.Author.Image.ShouldBe("https://example.com/author.jpg");
        articleResponse.Author.Following.ShouldBe(false);

        // Verify article was actually saved to database
        using var context = ScopedContext.Create(_connectionString);
        var savedArticle = await context.DbContext.Articles
            .Include(a => a.Tags)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == "how-to-train-your-dragon");

        savedArticle.ShouldNotBeNull();
        savedArticle.Title.ShouldBe("How to Train Your Dragon");
        savedArticle.Description.ShouldBe("Ever wonder how?");
        savedArticle.Body.ShouldBe("You have to believe in yourself and your dragon.");
        savedArticle.CreatedAt.ShouldBe(fixedTime);
        savedArticle.UpdatedAt.ShouldBe(fixedTime);
        savedArticle.Tags.Select(t => t.Name).OrderBy(n => n).ShouldBe(new[] { "dragons", "fantasy", "training" });
        savedArticle.Author.UserName.ShouldBe("testauthor");
    }
}