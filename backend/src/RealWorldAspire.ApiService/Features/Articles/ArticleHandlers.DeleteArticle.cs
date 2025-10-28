using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Articles;

public partial class ArticleHandlers
{
    public static Task<IResult> DeleteArticle(
        string slug,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext
    )
    {
        return Task.FromResult<IResult>(TypedResults.Ok());
    }
}