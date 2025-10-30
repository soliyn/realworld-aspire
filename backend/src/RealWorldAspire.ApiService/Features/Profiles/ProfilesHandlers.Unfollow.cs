using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Profiles;

public static partial class ProfilesHandlers
{
    public static async Task<IResult> Unfollow(
        string username,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        RealWorldDbContext dbContext
    )
    {
        var follower = await userManager.GetUserAsync(principal);
        var following = await userManager.FindByNameAsync(username);
        if (follower == null || following == null)
        {
            return TypedResults.NotFound(username);
        }

        var userFollow = await dbContext.UserFollows
            .FirstOrDefaultAsync(x => x.FollowerId == follower.Id && x.FollowingId == following.Id);
        if (userFollow == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.UserFollows.Remove(userFollow);
        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(new ProfileResponse()
        {
            Profile = new ProfileResponse.ProfileModel()
            {
                Username = following.UserName,
                Bio = following.Bio,
                Image = following.Image,
                Following = false,
            }
        });
    }
}