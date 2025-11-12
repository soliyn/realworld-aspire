# RealWorld API - .NET 10 with Aspire

A cloud-native implementation of the [RealWorld](https://realworld-docs.netlify.app/) blogging platform API spec, built with .NET 10 and .NET Aspire. This project demonstrates modern .NET development practices including minimal APIs, vertical slice architecture, and cloud-native patterns.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Development](#development)
- [Testing](#testing)
- [Architecture](#architecture)
- [Database](#database)
- [Authentication](#authentication)

## Overview

RealWorld is a Medium.com clone that demonstrates best practices for building production-ready fullstack applications. This backend implementation uses:

- **.NET 10** for modern C# features and performance
- **.NET Aspire** for cloud-native orchestration and observability
- **PostgreSQL** with Entity Framework Core for data persistence
- **Minimal APIs** for lightweight, high-performance endpoints
- **Vertical Slice Architecture** for feature-based code organization
- **JWT Authentication** for secure user sessions

## Features

### Core Functionality

- **User Management**
  - User registration and authentication
  - JWT token-based sessions
  - Profile management (bio, image, email updates)
  - User profiles with following status

- **Articles**
  - Create, read, update, and delete articles
  - Automatic URL-friendly slug generation
  - Rich filtering (by tag, author, or favorited users)
  - Pagination support
  - Personalized article feed from followed authors

- **Social Features**
  - Follow/unfollow users
  - Favorite/unfavorite articles
  - View follower counts and following status

- **Comments**
  - Add comments to articles
  - View all article comments
  - Delete own comments

- **Tags**
  - Tag articles with multiple labels
  - Browse all available tags
  - Filter articles by tag

## Technology Stack

### Core Framework
- .NET 10.0 with C# 14
- ASP.NET Core with Minimal APIs
- Entity Framework Core 10.0.0

### Database & Storage
- PostgreSQL (containerized via Aspire)
- EF Core with code-first migrations
- Npgsql provider with Aspire integration

### Authentication & Security
- JWT Bearer tokens (custom "Token" scheme for RealWorld spec)
- ASP.NET Core Identity for user management
- Configurable password requirements

### Cloud-Native & Observability
- .NET Aspire for orchestration
- OpenTelemetry for distributed tracing and metrics
- Health checks for readiness and liveness
- Service discovery and HTTP resilience

### Testing
- xUnit for test framework
- Shouldly for fluent assertions
- Testcontainers for isolated PostgreSQL instances
- Aspire.Hosting.Testing for integration tests
- Bogus for fake data generation

### Additional Libraries
- Slugify.Core for URL-friendly slugs
- Swashbuckle for OpenAPI/Swagger documentation
- Microsoft.Extensions.Http.Resilience for retry/circuit breaker patterns

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL container)
- A code editor (Visual Studio 2022, VS Code, or Rider)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd realworld-aspire/backend
   ```

2. **Configure JWT settings**

   Update `src/RealWorldAspire.ApiService/appsettings.json` with your JWT configuration:
   ```json
   {
     "JWT": {
       "SecretKey": "your-secret-key-here-must-be-at-least-256-bits-long",
       "Issuer": "RealWorldAspire",
       "Audience": "RealWorldUsers",
       "ExpirationInMinutes": 60
     }
   }
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/RealWorldAspire.AppHost/RealWorldAspire.AppHost.csproj
   ```

   This will:
   - Start PostgreSQL in a Docker container with persistent storage
   - Launch pgAdmin for database management
   - Run database migrations automatically
   - Seed sample data (in development mode)
   - Start the API service with health checks
   - Display the Aspire dashboard with all service endpoints

4. **Access the services**
   - **API**: Check the Aspire dashboard for the API service endpoint (typically `http://localhost:5000`)
   - **Swagger UI**: `http://localhost:5000/swagger` (development only)
   - **Aspire Dashboard**: Shown in terminal output
   - **pgAdmin**: Available through the Aspire dashboard

## Project Structure

```
backend/
├── src/
│   ├── RealWorldAspire.AppHost/              # Aspire orchestration host
│   │   ├── AppHost.cs                        # Service configuration
│   │   └── appsettings.json                  # Aspire settings
│   │
│   ├── RealWorldAspire.ApiService/           # Main API service
│   │   ├── Features/                         # Feature-based organization
│   │   │   ├── Articles/                     # Article CRUD, favorites, comments
│   │   │   ├── Users/                        # Registration and login
│   │   │   ├── User/  :                       # Current user operations
│   │   │   ├── Profiles/                     # User profiles and following
│   │   │   └── Tags/                         # Tag management
│   │   ├── Models/                           # Entity models
│   │   ├── Data/                             # DbContext and migrations
│   │   ├── Extensions/                       # Utility extensions
│   │   └── Program.cs                        # Application startup
│   │
│   └── RealWorldAspire.ServiceDefaults/      # Shared Aspire configuration
│       └── Extensions.cs                     # Health checks, telemetry, resilience
│
└── tests/
    ├── RealWorldAspire.ApiService.Tests/              # Unit/integration tests
    └── RealWorldAspire.ApiService.IntegrationTests/   # Full app tests
```

### Feature Organization

Each feature folder (e.g., `Features/Articles/`) contains:
- `*Endpoints.cs` - Route definitions using minimal APIs
- `*Handlers.cs` - Business logic and request handlers (sometimes split across multiple files)
- Request and Response DTOs specific to that feature

This **vertical slice architecture** keeps related code together, making features easier to understand and maintain.

## API Endpoints

All endpoints are prefixed with `/api`. Endpoints marked with [Auth] require authentication.

### Users
- `POST /api/users` - Register a new user
- `POST /api/users/login` - Login and receive JWT token

### Current User (Authenticated)
- [Auth] `GET /api/user` - Get current user details
- [Auth] `PUT /api/user` - Update user profile (email, bio, image)

### Articles
- `GET /api/articles` - List articles (supports filters: `tag`, `author`, `favorited`, `limit`, `offset`)
- [Auth] `POST /api/articles` - Create a new article
- `GET /api/articles/{slug}` - Get a single article by slug
- [Auth] `PUT /api/articles/{slug}` - Update an article
- [Auth] `DELETE /api/articles/{slug}` - Delete an article
- [Auth] `GET /api/articles/feed` - Get personalized feed from followed authors

### Favorites
- [Auth] `POST /api/articles/{slug}/favorite` - Add article to favorites
- [Auth] `DELETE /api/articles/{slug}/favorite` - Remove article from favorites

### Comments
- [Auth] `POST /api/articles/{slug}/comments` - Add a comment to an article
- `GET /api/articles/{slug}/comments` - Get all comments for an article
- [Auth] `DELETE /api/articles/{slug}/comments/{id}` - Delete a comment

### Profiles
- `GET /api/profiles/{username}` - Get a user's profile
- [Auth] `POST /api/profiles/{username}/follow` - Follow a user
- [Auth] `DELETE /api/profiles/{username}/follow` - Unfollow a user

### Tags
- `GET /api/tags` - Get all tags used in articles

### Health & Documentation
- `GET /health` - Health check endpoint
- `GET /alive` - Liveness check
- `GET /swagger` - Swagger UI (development only)
- `GET /openapi/v1.json` - OpenAPI specification

## Development

### Common Commands

```bash
# Build the entire solution
dotnet build

# Run the Aspire AppHost (starts all services)
dotnet run --project src/RealWorldAspire.AppHost/RealWorldAspire.AppHost.csproj

# Run just the API service (requires database to be running separately)
dotnet run --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj

# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/RealWorldAspire.ApiService.Tests/RealWorldAspire.ApiService.Tests.csproj

# Run only integration tests
dotnet test tests/RealWorldAspire.ApiService.IntegrationTests/RealWorldAspire.ApiService.IntegrationTests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests.GetArticle"
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj

# Apply migrations manually (in development, migrations run automatically on startup)
dotnet ef database update --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj

# Remove the last migration
dotnet ef migrations remove --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj
```

### Configuration Files

#### appsettings.json (API Service)
```json
{
  "JWT": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "RealWorldAspire",
    "Audience": "RealWorldUsers",
    "ExpirationInMinutes": 60
  },
  "ConnectionStrings": {
    "realworlddb": "Provided by Aspire"
  }
}
```

#### AppHost Configuration
The `AppHost.cs` file configures:
- PostgreSQL with persistent volume storage
- pgAdmin for database management
- Service references and dependencies
- Health check endpoints
- CORS policy for frontend integration

## Testing

### Unit Tests

The unit tests use **Testcontainers** to provide isolated PostgreSQL instances for each test class. This approach:
- Uses real database queries (no mocking DbContext)
- Provides isolation between test classes
- Runs fast with shared container per test class
- Tests actual SQL and database behavior

**Test Structure:**
```csharp
[Collection(nameof(PostgresDatabaseCollection))]
public class YourFeatureTests : HandlerTestBase, IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public YourFeatureTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Setup test data
    }

    [Fact]
    public async Task Should_Perform_Action()
    {
        // Arrange
        var request = new YourRequest();

        // Act
        var result = await ScopedContext.Execute(_fixture.ConnectionString,
            async (dbContext, userManager) =>
                await YourHandler.Method(request, dbContext, userManager)
        );

        // Assert
        result.ShouldBeOfType<Ok<YourResponse>>();
    }
}
```

### Integration Tests

Integration tests use **Aspire.Hosting.Testing** to test the entire distributed application:
- Starts the full AppHost with all services
- Tests real HTTP requests/responses
- Validates service-to-service communication
- Ensures health checks work correctly

**Example:**
```csharp
[Collection(nameof(AspireTestCollection))]
public class EndpointsTests
{
    private readonly AspireTestFixture _fixture;

    [Fact]
    public async Task Api_Returns_Health_Ok()
    {
        using var httpClient = _fixture.App.CreateHttpClient("apiservice");
        var response = await httpClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.Ok);
    }
}
```

### Running Tests

```bash
# All tests
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# Specific test class
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests"

# Tests matching a pattern
dotnet test --filter "DisplayName~Create"

# With code coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

## Architecture

### Vertical Slice Architecture

The codebase uses **vertical slices** instead of traditional horizontal layers. Each feature (Articles, Users, Profiles) contains all its related code:

**Benefits:**
- High cohesion: Related code stays together
- Easy to locate and modify features
- Clear boundaries between features
- Reduced coupling across features

**Example:**
```
Features/Articles/
├── ArticleEndpoints.cs        # Route definitions
├── ArticleHandlers.cs         # Business logic
├── CreateArticle.cs           # Handler for creating articles
├── GetArticles.cs             # Handler for listing articles
├── CreateArticleRequest.cs    # Request DTO
└── GetArticleResponse.cs      # Response DTO
```

### Minimal APIs

The project uses ASP.NET Core **Minimal APIs** instead of controllers:
- Lightweight and high-performance
- Less ceremony and boilerplate
- Clear endpoint definitions
- Easy to test

**Example Endpoint:**
```csharp
public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles").WithTags("Articles");

        group.MapGet("", ArticleHandlers.GetArticles);
        group.MapPost("", ArticleHandlers.CreateArticle).RequireAuthorization();
        group.MapGet("{slug}", ArticleHandlers.GetArticle);

        return app;
    }
}
```

### Handler Pattern

Handlers are static methods that receive dependencies via parameters:

```csharp
public static async Task<IResult> CreateArticle(
    CreateArticleRequest request,
    ClaimsPrincipal principal,
    UserManager<AppUser> userManager,
    RealWorldDbContext dbContext,
    TimeProvider timeProvider)
{
    // Validate user
    var user = await userManager.GetUserAsync(principal);
    if (user == null) return TypedResults.Unauthorized();

    // Business logic
    var article = new Article
    {
        Title = request.Article.Title,
        // ... map properties
        CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        AuthorId = user.Id
    };

    dbContext.Articles.Add(article);
    await dbContext.SaveChangesAsync();

    // Return typed result
    return TypedResults.Ok(new GetArticleResponse { Article = /* map */ });
}
```

### Dependency Injection

Common services injected into handlers:
- `ClaimsPrincipal` - Current authenticated user claims
- `UserManager<AppUser>` - ASP.NET Core Identity user operations
- `RealWorldDbContext` - Entity Framework database context
- `TimeProvider` - Testable time abstraction
- `IConfiguration` - Application configuration

## Database

### Entity Model

**Core Entities:**

- **AppUser** (extends IdentityUser)
  - Username, Email, Bio, Image
  - Navigation: Articles, Comments, Followers, Following, FavoritedArticles

- **Article**
  - Slug, Title, Description, Body
  - CreatedAt, UpdatedAt (stored as UTC)
  - Navigation: Author, Tags, Comments, FavoritedByUsers

- **Comment**
  - Body, CreatedAt, UpdatedAt
  - Navigation: Author, Article

- **Tag**
  - Name
  - Navigation: Articles (many-to-many)

- **UserFollow** (junction table)
  - FollowerId, FollowingId
  - Navigation: Follower, Following

- **FavoriteArticle** (junction table)
  - ArticleId, FavoritedByUsersId

### Relationships

- **User -> Articles**: One-to-Many (author)
- **User -> Comments**: One-to-Many (author)
- **User <-> User**: Many-to-Many (followers, via UserFollow)
- **User <-> Articles**: Many-to-Many (favorites, via FavoriteArticle)
- **Article <-> Tags**: Many-to-Many
- **Article -> Comments**: One-to-Many

### DateTime Handling

All DateTime properties are explicitly converted to UTC to ensure PostgreSQL compatibility:

```csharp
modelBuilder.Entity<Article>()
    .Property(x => x.CreatedAt)
    .HasConversion(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    );
```

### Migrations

Migrations are located in `src/RealWorldAspire.ApiService/Migrations/`:
- `InitialCreate` - Core entities
- `AddUserFollow` - Following relationships
- `AddTags` - Tag entity
- `AddComment` - Comment entity
- `FavoriteArticle` - Favorite relationships

**In development**, migrations run automatically on startup. In production, run manually:
```bash
dotnet ef database update --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj
```

## Authentication

### JWT Token Authentication

The API uses JWT Bearer tokens with a custom scheme to match the RealWorld spec:
- Header format: `Authorization: Token <jwt>` (not `Bearer <jwt>`)
- Token contains user ID, username, email, and optional roles
- Configurable expiration time

### Configuration

In `appsettings.json`:
```json
{
  "JWT": {
    "SecretKey": "your-secret-key-must-be-at-least-256-bits-long-for-HS256",
    "Issuer": "RealWorldAspire",
    "Audience": "RealWorldUsers",
    "ExpirationInMinutes": 60
  }
}
```

### Token Generation

The `JwtTokenService` creates tokens with the following claims:
- `NameIdentifier` - User ID
- `Name` - Username
- `Email` - User email
- `Jti` - Unique token identifier
- `Role` - User roles (if any)

### User Registration

Users are created through ASP.NET Core Identity with custom password requirements:
- Minimum 6 characters
- Must contain at least one digit
- Must contain at least one uppercase letter

### Using Authentication in Handlers

```csharp
public static async Task<IResult> ProtectedEndpoint(
    ClaimsPrincipal principal,
    UserManager<AppUser> userManager,
    RealWorldDbContext dbContext)
{
    // Get the current user
    var user = await userManager.GetUserAsync(principal);
    if (user == null)
        return TypedResults.Unauthorized();

    // Use authenticated user for business logic
    // ...
}
```

## Contributing

This project follows the [RealWorld API Spec](https://realworld-docs.netlify.app/specifications/backend/endpoints/). When contributing:

1. Ensure all tests pass: `dotnet test`
2. Follow existing code organization patterns (vertical slices)
3. Add tests for new features
4. Update this README if adding new functionality
5. Respect the TreatWarningsAsErrors setting (all warnings must be fixed)

## Resources

- [RealWorld Spec](https://realworld-docs.netlify.app/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
