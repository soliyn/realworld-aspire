using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using RealWorldAspire.ApiService.Features.User;
using RealWorldAspire.ApiService.Features.Users;

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

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // Configure password requirements
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    
        // Configure lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    
        // Require unique email
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<RealWorldDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ??  throw new NullReferenceException());

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
                {
                    var authHeader = value.ToString();

                    const string tokenScheme = "Token ";
                    if (authHeader.StartsWith(tokenScheme, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring(tokenScheme.Length);
                    }
                }
            
                return Task.CompletedTask;
            },
        
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddTransient<JwtTokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var appApi = app.MapGroup("/api");
appApi
    .MapArticleEndpoints()
    .MapUserEndpoints()
    .MapUsersEndpoints()
    ;

if (app.Environment.IsDevelopment())
{
    // Ensure database is created and seeded
    using var scope = app.Services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<RealWorldDbContext>();
    context.Database.Migrate();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
