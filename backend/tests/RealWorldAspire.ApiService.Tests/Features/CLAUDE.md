# Unit Testing Guidelines for RealWorldAspire.ApiService.Tests

This document provides guidance on writing unit tests for the RealWorldAspire.ApiService project using the patterns established in this test suite.

## Testing Approach

This test suite uses **real database integration tests** rather than mocks, providing more reliable and realistic testing. Tests use PostgreSQL via Testcontainers for isolation and reproducibility.

## Test Structure Pattern

### 1. Test Class Setup

All handler test classes should follow this pattern:

```csharp
[Collection(nameof(PostgresDatabaseCollection))]
public class YourHandlerTests : HandlerTestBase, IAsyncLifetime
{
    private readonly ClaimsPrincipal _principal;
    private readonly string _connectionString;

    public YourHandlerTests(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
        _principal = new ClaimsPrincipal();
    }

    public async Task InitializeAsync()
    {
        // Setup test data
        using var context = ScopedContext.Create(_connectionString);

        // Create users
        var user = new AppUser()
        {
            Email = "test@example.com",
            UserName = "TestUser",
            Bio = "Test bio",
            Image = "https://example.com/image.jpg",
        };
        await context.UserManager.CreateAsync(user);

        // Create other test data
        await context.DbContext.AddRangeAsync(GetTestData(user));
        await context.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

### 2. Key Components

- **`[Collection(nameof(PostgresDatabaseCollection))]`**: Ensures tests share the same database instance within a test run
- **`HandlerTestBase`**: Base class providing common test utilities
- **`IAsyncLifetime`**: Provides setup (`InitializeAsync`) and teardown (`DisposeAsync`) hooks
- **`PostgresTestFixture`**: Provides the PostgreSQL test database connection
- **`ScopedContext`**: Helper for managing DbContext and UserManager instances

### 3. Test Method Pattern

```csharp
[Fact]
public async Task Should_Do_Something()
{
    // Arrange
    var request = new YourRequest()
    {
        // Setup request parameters
    };

    // Act
    var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
        await YourHandler.YourMethod(request, _principal, userManager, dbContext)
    );

    // Assert
    result.ShouldBeOfType<Ok<YourResponse>>();
    var okResult = result as Ok<YourResponse>;
    okResult.ShouldNotBeNull();

    // Additional assertions
    okResult.Value.ShouldNotBeNull();
    okResult.Value.SomeProperty.ShouldBe(expectedValue);
}
```

## Testing Authenticated Users

When testing endpoints that require authentication, create a `ClaimsPrincipal` using `SignInManager`:

```csharp
[Fact]
public async Task Should_Handle_Authenticated_Request()
{
    // Arrange
    var user = new AppUser
    {
        UserName = "authenticateduser",
        Email = "auth@example.com",
    };

    ClaimsPrincipal principal;
    using (var context = ScopedContext.Create(_connectionString))
    {
        await context.UserManager.CreateAsync(user);
        user = (await context.UserManager.FindByEmailAsync(user.Email))!;

        // Create authenticated principal
        principal = await context.SignInManager.CreateUserPrincipalAsync(user);
    }

    // Act
    var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
        await YourHandler.YourMethod(principal, userManager, dbContext)
    );

    // Assert
    // ...
}
```

## Creating Test Data

### Approach: Clarity Over Cleverness

Create test data using a clear, explicit approach:

1. Generate base data in a loop
2. Modify specific items by index after creation

**Example:**

```csharp
private List<Article> GetFakeArticles(AppUser author1, AppUser author2)
{
    var articles = new List<Article>();

    // Create 20 articles
    for (int i = 1; i <= 20; i++)
    {
        articles.Add(new Article
        {
            Slug = $"article-{i}",
            Title = $"Article {i}",
            Description = $"Description for article {i}",
            Body = $"Body content for article {i}",
            CreatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
            UpdatedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
            FavoritedByUsers = [],
            Tags = [],
            Author = author1,
        });
    }

    // Modify specific articles for test scenarios
    // Assign articles 11-13 to second author
    articles[10].Author = author2;
    articles[11].Author = author2;
    articles[12].Author = author2;

    // Add tags to specific articles
    articles[0].Tags.Add(new Tag { Name = "javascript" });
    articles[1].Tags.Add(new Tag { Name = "javascript" });
    articles[2].Tags.Add(new Tag { Name = "javascript" });

    return articles;
}
```

This approach makes it immediately clear:
- Which articles have which properties
- What test scenarios are being set up
- Easy to modify and maintain

## Testing Relationships

### Testing Favorites

```csharp
var favoritingUser = new AppUser { /* ... */ };
await context.UserManager.CreateAsync(favoritingUser);
favoritingUser = (await context.UserManager.FindByEmailAsync(favoritingUser.Email))!;

var article = await context.DbContext.Articles
    .FirstOrDefaultAsync(a => a.Slug == "some-slug");
article.FavoritedByUsers.Add(favoritingUser);
await context.DbContext.SaveChangesAsync();
```

### Testing Follows

```csharp
var follower = new AppUser { /* ... */ };
var following = new AppUser { /* ... */ };

await context.UserManager.CreateAsync(follower);
await context.UserManager.CreateAsync(following);

follower = (await context.UserManager.FindByEmailAsync(follower.Email))!;
following = (await context.UserManager.FindByEmailAsync(following.Email))!;

var userFollow = new UserFollow
{
    FollowerId = follower.Id,
    FollowingId = following.Id
};
await context.DbContext.UserFollows.AddAsync(userFollow);
await context.DbContext.SaveChangesAsync();
```

## Common Assertions

Using Shouldly for fluent assertions:

```csharp
// Type assertions
result.ShouldBeOfType<Ok<Response>>();
result.ShouldBeOfType<NotFound>();

// Value assertions
value.ShouldBe(expected);
value.ShouldNotBeNull();
value.ShouldBeNull();

// Collection assertions
collection.ShouldContain(item);
collection.Count.ShouldBe(expectedCount);
collection.ShouldBeSubsetOf(expectedItems);

// Object comparison
actual.ShouldBeEquivalentTo(expected);
```

## Example Test Classes

Refer to these examples for patterns:

- **GetArticle.cs**: Shows testing with authenticated users, favorites, and follows
- **GetArticles.cs**: Shows testing filters (tag, author, favorited) and pagination
- **Unfollow.cs**: Shows testing user relationships

## Best Practices

1. **Use Real Database**: Don't mock `DbContext` or `UserManager`
2. **Clear Test Data**: Use `InitializeAsync` to set up clean, isolated test data
3. **Descriptive Names**: Test method names should clearly describe what they test
4. **Index-Based Modifications**: When setting up test data, create all items first, then modify by index
5. **Explicit Assertions**: Assert specific expected values rather than just checking for non-null
6. **Test One Thing**: Each test should verify one specific behavior
7. **Arrange-Act-Assert**: Follow the AAA pattern consistently

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests.GetArticle"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~GetArticle.Should_Return_Article"
```

## Required Using Statements

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.YourFeature;
using Shouldly;
using RealWorldAspire.ApiService.Tests.TestUtilities;
```

## Why This Approach?

1. **Real Database**: Catches issues that mocks would miss (SQL queries, relationships, constraints)
2. **Testcontainers**: Provides isolated, reproducible test environment
3. **Clear Test Data**: Index-based modifications make test scenarios obvious
4. **Fast Feedback**: Tests run quickly despite using real database
5. **Maintainable**: Easy to understand and modify tests

## Anti-Patterns to Avoid

❌ **Don't** mock `DbContext` or `UserManager`
✅ **Do** use real database via `ScopedContext`

❌ **Don't** use complex conditional logic in test data setup
✅ **Do** create data first, modify by index

❌ **Don't** return empty collections from test data generators
✅ **Do** create realistic test data

❌ **Don't** use vague assertions like `ShouldNotBeNull()`
✅ **Do** assert specific expected values

❌ **Don't** share state between tests
✅ **Do** use `InitializeAsync` for clean setup

## Summary

This testing approach prioritizes:
- **Reliability**: Real database catches real issues
- **Clarity**: Explicit test data setup is easy to understand
- **Maintainability**: Clear patterns make tests easy to modify
- **Speed**: Testcontainers provides fast, isolated tests

Follow these patterns for consistent, reliable unit tests.
