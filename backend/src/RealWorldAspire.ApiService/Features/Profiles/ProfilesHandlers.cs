using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Profiles;

public static class ProfilesHandlers
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

    public static async Task<IResult> Follow(
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

        if (following.Id == follower.Id)
        {
            return TypedResults.BadRequest("You cannot follow yourself");
        }

        var userFollow = await dbContext.UserFollows
            .FirstOrDefaultAsync(x => x.FollowerId == follower.Id && x.FollowingId == following.Id);
        if (userFollow != null)
        {
            return TypedResults.Ok();
        }

        userFollow = new UserFollow()
        {
            FollowerId = follower.Id,
            FollowingId = following.Id,
        };
        await dbContext.UserFollows.AddAsync(userFollow);
        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(new ProfileResponse()
        {
            Profile = new ProfileResponse.ProfileModel()
            {
                Username = following.UserName,
                Bio = following.Bio,
                Image = following.Image,
                Following = true,
            }
        });
    }
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