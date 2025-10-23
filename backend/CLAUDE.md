# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9 implementation of the RealWorld API spec using .NET Aspire for cloud-native application development. The solution implements a conduit-style blogging platform with articles, user authentication, profiles, and social features.

## Project Structure

The solution uses .NET Aspire's recommended architecture with three main projects:

- **RealWorldAspire.AppHost**: Aspire orchestration host that configures the distributed application, PostgreSQL database with pgAdmin, and service references
- **RealWorldAspire.ApiService**: Main API service implementing the RealWorld spec with minimal API endpoints
- **RealWorldAspire.ServiceDefaults**: Shared Aspire service defaults including resilience, service discovery, and OpenTelemetry

## Common Commands

### Build and Run
```bash
# Build the entire solution
dotnet build

# Run the Aspire AppHost (starts all services including PostgreSQL)
dotnet run --project src/RealWorldAspire.AppHost/RealWorldAspire.AppHost.csproj

# Run just the API service (requires database to be running separately)
dotnet run --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj
```

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj

# Apply migrations (in development, migrations run automatically on startup)
dotnet ef database update --project src/RealWorldAspire.ApiService/RealWorldAspire.ApiService.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/RealWorldAspire.ApiService.Tests/RealWorldAspire.ApiService.Tests.csproj

# Run integration tests only
dotnet test tests/RealWorldAspire.ApiService.IntegrationTests/RealWorldAspire.ApiService.IntegrationTests.csproj

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~ArticleHandlersTests.GetArticle"
```

## Architecture Patterns

### Feature-Based Organization
The API service uses vertical slice architecture where features are organized by domain area in `Features/`:
- **Articles**: Article CRUD operations, feed, favorites, tags
- **Users**: User registration and login
- **User**: Current user operations (get/update authenticated user)
- **Profiles**: User profile viewing and following

Each feature folder contains:
- `*Endpoints.cs`: Endpoint route definitions using minimal APIs
- `*Handlers.cs`: Business logic and request handlers
- Request/Response DTOs specific to that feature

### Database Context
- Uses `RealWorldDbContext` which extends `IdentityDbContext<AppUser>` for ASP.NET Core Identity integration
- Entity configurations are in `OnModelCreating` method
- Key entities: `Article`, `Author`, `AppUser`, `UserFollow`, `FavoriteArticle`
- DateTime properties are explicitly converted to UTC for PostgreSQL compatibility

### Authentication & Authorization
- JWT Bearer token authentication with custom "Token" scheme support (not "Bearer")
- JWT configuration in `appsettings.json` under "JWT" section (SecretKey, Issuer, Audience, ExpirationInMinutes)
- `JwtTokenService` generates tokens for authenticated users
- ASP.NET Core Identity handles user management with custom password requirements

### Aspire Integration
- AppHost in `AppHost.cs` configures PostgreSQL with persistent volume and pgAdmin
- Database connection named "realworlddb" is referenced from AppHost to ApiService
- Service defaults provide health checks, resilience, telemetry
- In development, database seeding occurs automatically via `DataSeeder.cs`

## Testing Patterns

### Unit Tests (RealWorldAspire.ApiService.Tests)
- Uses xUnit, Moq, Shouldly, and Bogus for fake data generation
- Tests organized by feature matching the source structure: `Features/Articles/ArticleHandlersTests/`
- Uses MockQueryable.Moq for mocking DbSet and IQueryable

### Integration Tests (RealWorldAspire.ApiService.IntegrationTests)
- Uses Aspire.Hosting.Testing for full distributed application testing
- Tests the entire AppHost including database and API service

## Important Configuration Notes

- **TreatWarningsAsErrors**: Enabled in `Directory.Build.props` - all warnings must be fixed
- **SDK Version**: Uses .NET 9 with preview features enabled (`global.json`)
- **Nullable**: Enabled across all projects - null reference types are enforced
- **Swagger**: Available in development at `/swagger` with custom schema ID generation using full type names to avoid collisions
- **Health Checks**: Configured at `/health` endpoint for the API service
- **CORS**: Not currently configured (may need to be added for frontend integration)

## Development Workflow

1. The AppHost automatically starts PostgreSQL in a container with persistent storage
2. In development mode, migrations run automatically on API service startup (`Program.cs:142`)
3. Database seeding occurs after migrations if running in development
4. Custom authentication header extraction supports both "Bearer" and "Token" prefix schemes
5. API endpoints are grouped under `/api` prefix
6. API routes follow RealWorld spec conventions (e.g., `/api/articles`, `/api/user`, `/api/profiles/:username`)
