using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Profiles;

public static partial class ProfilesHandlers
{
    public static async Task<IResult> GetProfile(
        string username,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext
    )
    {
        var currentUser = await userManager.GetUserAsync(principal);
        if (currentUser == null)
        {
            return Results.Unauthorized();
        }
        var user = await dbContext.Users
            .Where(u => u.UserName == username)
            .Select(x => new { x.Id, x.UserName, x.Bio, x.Image, Following = x.Followers.Any(uf => uf.FollowerId == currentUser.Id) })
            .FirstOrDefaultAsync()
        ;
        if (user == null)
        {
            return TypedResults.NotFound(username);
        }

        return TypedResults.Ok(new ProfileResponse()
        {
            Profile = new ProfileResponse.ProfileModel()
            {
                Username = user.UserName,
                Bio = user.Bio,
                Image = user.Image,
                Following = user.Following,
            }
        });
    }
}