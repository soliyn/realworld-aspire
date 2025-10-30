using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Tests.TestUtilities;

public class ScopedContext : IDisposable
{
    public static ScopedContext Create(string connectionString)
    {
        return new ScopedContext(connectionString);
    }

    public static async Task<IResult> Execute(
        string connectionString, 
        Func<RealWorldDbContext, Task<IResult>> executeFunc
    )
    {
        using var context = Create(connectionString);
        return await executeFunc(context._dbContext);
    }

    public static async Task<IResult> Execute(
        string connectionString,
        Func<RealWorldDbContext, UserManager<AppUser>, Task<IResult>> executeFunc
    )
    {
        using var context = Create(connectionString);
        return await executeFunc(context._dbContext, context.UserManager);
    }

    private readonly ServiceProvider _serviceProvider;
    private readonly RealWorldDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public ServiceProvider ServiceProvider => _serviceProvider;
    public RealWorldDbContext DbContext => _dbContext;
    public UserManager<AppUser> UserManager => _userManager;
    public SignInManager<AppUser> SignInManager => _signInManager;

    private ScopedContext(string connectionString)
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<RealWorldDbContext>(options =>
            options.UseNpgsql(connectionString)
        );
        
        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<RealWorldDbContext>()
            .AddDefaultTokenProviders();
        
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        });
        
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        
        _dbContext = _serviceProvider.GetRequiredService<RealWorldDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<AppUser>>();
        _signInManager = _serviceProvider.GetRequiredService<SignInManager<AppUser>>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        _dbContext?.Dispose();
        _userManager?.Dispose();
    }
}