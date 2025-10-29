using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using Shouldly;
using RealWorldAspire.ApiService.Tests.TestUtilities;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class GetFeed : HandlerTestBase, IAsyncLifetime
{
    private readonly string _connectionString;

    public GetFeed(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        using var context = ScopedContext.Create(_connectionString);

        // Create three authors
        var author1 = new AppUser()
        {
            Email = "author1@example.com",
            UserName = "Author1",
            Bio = "Tech writer and blogger",
            Image = "https://example.com/author1.jpg",
        };
        await context.UserManager.CreateAsync(author1);

        var author2 = new AppUser()
        {
            Email = "author2@example.com",
            UserName = "Author2",
            Bio = "JavaScript enthusiast",
            Image = "https://example.com/author2.jpg",
        };
        await context.UserManager.CreateAsync(author2);

        var author3 = new AppUser()
        {
            Email = "author3@example.com",
            UserName = "Author3",
            Bio = "Full-stack developer",
            Image = "https://example.com/author3.jpg",
        };
        await context.UserManager.CreateAsync(author3);

        // Create articles from all three authors
        var articles = GetFakeArticles(author1, author2, author3);
        await context.DbContext.AddRangeAsync(articles);
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Return_Articles_From_Followed_Users()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "currentuser",
            Email = "current@example.com",
            Bio = "I follow great authors",
            Image = "https://example.com/current.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            // Follow Author1 and Author2, but not Author3
            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            var author2 = await context.UserManager.FindByEmailAsync("author2@example.com");

            author1.ShouldNotBeNull();
            author2.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddRangeAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id },
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author2.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest();

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;
        var count = okResult.Value?.ArticlesCount;

        articles.ShouldNotBeNull();
        count.ShouldBe(20); // 10 from Author1 + 10 from Author2

        // Verify all articles are from Author1 or Author2, none from Author3
        foreach (var article in articles)
        {
            article.Author.Username.ShouldBeOneOf("Author1", "Author2");
            article.Author.Username.ShouldNotBe("Author3");
            article.Author.Following.ShouldBeTrue(); // All authors in feed should be followed
        }
    }

    [Fact]
    public async Task Should_Return_Articles_Ordered_By_CreatedAt_Descending()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "ordereduser",
            Email = "ordered@example.com",
            Bio = "Testing order",
            Image = "https://example.com/ordered.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            author1.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest { Limit = 10 };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;

        articles.ShouldNotBeNull();

        // Verify articles are ordered by CreatedAt descending
        for (int i = 0; i < articles.Count - 1; i++)
        {
            articles[i].CreatedAt.ShouldBeGreaterThanOrEqualTo(articles[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task Should_Support_Pagination_With_Limit()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "limituser",
            Email = "limit@example.com",
            Bio = "Testing limits",
            Image = "https://example.com/limit.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            author1.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest { Limit = 5 };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;
        var count = okResult.Value?.ArticlesCount;

        articles.ShouldNotBeNull().Count.ShouldBe(5);
        count.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Support_Pagination_With_Offset()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "offsetuser",
            Email = "offset@example.com",
            Bio = "Testing offset",
            Image = "https://example.com/offset.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            author1.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest { Limit = 5, Offset = 5 };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;

        articles.ShouldNotBeNull().Count.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Use_Default_Limit_Of_20_When_Not_Specified()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "defaultuser",
            Email = "default@example.com",
            Bio = "Testing defaults",
            Image = "https://example.com/default.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            var author2 = await context.UserManager.FindByEmailAsync("author2@example.com");
            author1.ShouldNotBeNull();
            author2.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddRangeAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id },
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author2.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest(); // No limit specified

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;
        var count = okResult.Value?.ArticlesCount;

        articles.ShouldNotBeNull().Count.ShouldBe(20); // Default limit
        count.ShouldBe(20);
    }

    [Fact]
    public async Task Should_Return_Empty_Feed_When_Not_Following_Anyone()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "lonelyuser",
            Email = "lonely@example.com",
            Bio = "Not following anyone",
            Image = "https://example.com/lonely.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest();

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;
        var count = okResult.Value?.ArticlesCount;

        articles.ShouldNotBeNull().Count.ShouldBe(0);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Mark_Favorited_Articles_Correctly()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "favoriteuser",
            Email = "favorite@example.com",
            Bio = "I favorite articles",
            Image = "https://example.com/favorite.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            author1.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id }
            );

            // Favorite some articles from author1
            var articlesToFavorite = context.DbContext.Articles
                .Where(a => a.Author.Email == "author1@example.com")
                .Take(3)
                .ToList();

            foreach (var article in articlesToFavorite)
            {
                article.FavoritedByUsers.Add(currentUser);
            }

            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest();

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;

        articles.ShouldNotBeNull();

        // Verify that some articles are favorited and some are not
        var favoritedArticles = articles.Where(a => a.Favorited).ToList();
        var notFavoritedArticles = articles.Where(a => !a.Favorited).ToList();

        favoritedArticles.Count.ShouldBe(3);
        notFavoritedArticles.Count.ShouldBe(7); // 10 articles from author1, 3 favorited
    }

    [Fact]
    public async Task Should_Include_Article_Metadata()
    {
        // Arrange
        var currentUser = new AppUser
        {
            UserName = "metadatauser",
            Email = "metadata@example.com",
            Bio = "Testing metadata",
            Image = "https://example.com/metadata.jpg",
        };

        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            await context.UserManager.CreateAsync(currentUser);
            currentUser = (await context.UserManager.FindByEmailAsync(currentUser.Email))!;

            var author1 = await context.UserManager.FindByEmailAsync("author1@example.com");
            author1.ShouldNotBeNull();

            await context.DbContext.UserFollows.AddAsync(
                new UserFollow { FollowerId = currentUser.Id, FollowingId = author1.Id }
            );
            await context.DbContext.SaveChangesAsync();

            principal = await context.SignInManager.CreateUserPrincipalAsync(currentUser);
        }

        var request = new GetFeedRequest { Limit = 1 };

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ArticleHandlers.GetFeed(request, principal, userManager, dbContext)
        );

        // Assert
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var articles = okResult.Value?.Articles;

        articles.ShouldNotBeNull().Count.ShouldBe(1);
        var article = articles.First();

        // Verify all expected properties are populated
        article.Slug.ShouldNotBeNullOrEmpty();
        article.Title.ShouldNotBeNullOrEmpty();
        article.Description.ShouldNotBeNullOrEmpty();
        article.TagList.ShouldNotBeNull();
        article.CreatedAt.ShouldNotBe(default);
        article.UpdatedAt.ShouldNotBe(default);
        article.FavoritesCount.ShouldBeGreaterThanOrEqualTo(0);

        // Verify author metadata
        article.Author.ShouldNotBeNull();
        article.Author.Username.ShouldBe("Author1");
        article.Author.Bio.ShouldBe("Tech writer and blogger");
        article.Author.Image.ShouldBe("https://example.com/author1.jpg");
        article.Author.Following.ShouldBeTrue();
    }

    private List<Article> GetFakeArticles(AppUser author1, AppUser author2, AppUser author3)
    {
        var articles = new List<Article>();

        // Create 10 articles from each author (30 total)
        for (int i = 1; i <= 10; i++)
        {
            articles.Add(new Article
            {
                Slug = $"author1-article-{i}",
                Title = $"Author1 Article {i}",
                Description = $"Description for Author1 article {i}",
                Body = $"Body content for Author1 article {i}",
                CreatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                UpdatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                FavoritedByUsers = [],
                Tags = [new Tag { Name = "tag1" }],
                Author = author1,
            });
        }

        for (int i = 1; i <= 10; i++)
        {
            articles.Add(new Article
            {
                Slug = $"author2-article-{i}",
                Title = $"Author2 Article {i}",
                Description = $"Description for Author2 article {i}",
                Body = $"Body content for Author2 article {i}",
                CreatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i + 10),
                UpdatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i + 10),
                FavoritedByUsers = [],
                Tags = [new Tag { Name = "tag2" }],
                Author = author2,
            });
        }

        for (int i = 1; i <= 10; i++)
        {
            articles.Add(new Article
            {
                Slug = $"author3-article-{i}",
                Title = $"Author3 Article {i}",
                Description = $"Description for Author3 article {i}",
                Body = $"Body content for Author3 article {i}",
                CreatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i + 20),
                UpdatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i + 20),
                FavoritedByUsers = [],
                Tags = [new Tag { Name = "tag3" }],
                Author = author3,
            });
        }

        return articles;
    }
}