using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<RealWorldDbContext>("realworlddb", configureDbContextOptions: options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSeeding((context, _) => context.Seed());
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var appApi = app.MapGroup("/api");
appApi.MapArticleEndpoints();

if (app.Environment.IsDevelopment())
{
    // Ensure database is created and seeded
    using var scope = app.Services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<RealWorldDbContext>();
    context.Database.Migrate();
}

app.MapDefaultEndpoints();

app.Run();
