using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using Shouldly;
using RealWorldAspire.ApiService.Tests.TestUtilities;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class GetArticle : HandlerTestBase, IAsyncLifetime
{
    private readonly ClaimsPrincipal _principal;
    private readonly string _connectionString;

    public GetArticle(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();

        _principal = new ClaimsPrincipal();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);
        var author = new AppUser()
        {
            Email = "author@example.com",
            UserName = "Author",
            Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
            Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
        };
        await context.UserManager.CreateAsync(author);

        await context.DbContext.AddRangeAsync(GetFakeArticles(author));
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Return_Article()
    {
        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetArticle("how-to-learn-javascript-efficiently", _principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse()
            {
                Article = new GetArticleResponse.ArticleModel()
                {
                    Slug = "how-to-learn-javascript-efficiently",
                    Title = "How to Learn JavaScript Efficiently",
                    Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                    Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                    TagList = ["beginners", "javascript", "programming", "webdev"],
                    CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    Favorited = false,
                    FavoritesCount = 0,
                    Author = new GetArticleResponse.ArticleModel.AuthorDto
                    {
                        Username = "Author",
                        Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                        Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                        Following = false,
                    },
                },
            });
    }

    [Fact]
    public async Task Should_Return_404NotFound()
    {
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetArticle("non-existent-slug", _principal, userManager, dbContext)
        );
        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task Should_Return_Favorited_Article()
    {
        // Arrange
        var favoritingUser = new AppUser
        {
            UserName = "favoriter",
            Email = "favoriter@example.com",
            Bio = "Loves JavaScript articles",
            Image = "https://example.com/image.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(favoritingUser);
            favoritingUser = (await context.UserManager.FindByEmailAsync(favoritingUser.Email))!;

            var article = await context.DbContext.Articles
                .FirstOrDefaultAsync(a => a.Slug == "how-to-learn-javascript-efficiently");
            article.ShouldNotBeNull();
            article.FavoritedByUsers.Add(favoritingUser);
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(favoritingUser);
        }

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetArticle("how-to-learn-javascript-efficiently", principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse()
            {
                Article = new GetArticleResponse.ArticleModel()
                {
                    Slug = "how-to-learn-javascript-efficiently",
                    Title = "How to Learn JavaScript Efficiently",
                    Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                    Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                    TagList = ["beginners", "javascript", "programming", "webdev"],
                    CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    Favorited = true,
                    FavoritesCount = 1,
                    Author = new GetArticleResponse.ArticleModel.AuthorDto
                    {
                        Username = "Author",
                        Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                        Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                        Following = false,
                    },
                },
            });
    }

    [Fact]
    public async Task Should_Return_Article_With_Following_Author()
    {
        // Arrange
        var followerUser = new AppUser
        {
            UserName = "follower",
            Email = "follower@example.com",
            Bio = "I follow great authors",
            Image = "https://example.com/follower.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(followerUser);
            followerUser = (await context.UserManager.FindByEmailAsync(followerUser.Email))!;

            var author = await context.UserManager.FindByEmailAsync("author@example.com");
            author.ShouldNotBeNull();

            var userFollow = new UserFollow
            {
                FollowerId = followerUser.Id,
                FollowingId = author.Id,
            };
            await context.DbContext.UserFollows.AddAsync(userFollow);
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(followerUser);
        }

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetArticle("how-to-learn-javascript-efficiently", principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse()
            {
                Article = new GetArticleResponse.ArticleModel()
                {
                    Slug = "how-to-learn-javascript-efficiently",
                    Title = "How to Learn JavaScript Efficiently",
                    Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                    Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                    TagList = ["beginners", "javascript", "programming", "webdev"],
                    CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    Favorited = false,
                    FavoritesCount = 0,
                    Author = new GetArticleResponse.ArticleModel.AuthorDto
                    {
                        Username = "Author",
                        Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                        Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                        Following = true,
                    },
                },
            });
    }

    private List<Article> GetFakeArticles(AppUser author) =>
    [
        new()
        {
            Slug = "how-to-learn-javascript-efficiently",
            Title = "How to Learn JavaScript Efficiently",
            Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
            Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
            CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
            FavoritedByUsers = [],
            Tags = ((List<string>)["beginners", "javascript", "programming", "webdev"])
                .Select(x => new Tag() { Name = x })
                .ToList(),
            Author = author,
        },

        new()
        {
            Slug = "advanced-csharp-tips",
            Title = "Advanced C# Tips",
            Description = "Take your C# skills to the next level with these advanced tips and tricks.",
            Body = "C# offers many advanced features for experienced developers.\n\n## Use LINQ Effectively\n\nLINQ can simplify complex data queries.\n\n## Master Async Programming\n\nAsync/await helps you write scalable applications.\n\n## Explore Span<T>\n\nSpan<T> enables high-performance memory access.",
            CreatedAt = new DateTime(2025, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 10, 10, 12, 30, 0, DateTimeKind.Utc),
            FavoritedByUsers = [],
            Tags = [],
            Author = author,
        },
    ];

}