using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using Testcontainers.PostgreSql;

namespace RealWorldAspire.ApiService.Tests.TestUtilities;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17.6")
        .Build();
    private readonly DbContextOptionsBuilder<RealWorldDbContext> _optionsBuilder = new();
    
    public PostgreSqlContainer Postgres => _postgres;
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _optionsBuilder.UseNpgsql(_postgres.GetConnectionString());
        await using RealWorldDbContext dbContext = new RealWorldDbContext(_optionsBuilder.Options);
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }
}