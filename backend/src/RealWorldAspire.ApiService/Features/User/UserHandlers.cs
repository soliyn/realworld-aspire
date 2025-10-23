using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Users;

namespace RealWorldAspire.ApiService.Features.User;

public static class UserHandlers
{
    public static async Task<IResult> Get(ClaimsPrincipal principal, UserManager<AppUser> userManager, JwtTokenService tokenService)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user == null)
        {
            return TypedResults.Unauthorized();
        }
        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.GenerateToken(user, roles);
        return TypedResults.Ok(new UserResponse
        {
            User = new UserResponse.UserModel
            {
                Username = user.UserName,
                Email = user.Email,
                Token = token,
            }
        });  
    }
    
    public static async Task<IResult> Update(
        UpdateUserRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        JwtTokenService tokenService
    )
    {
        var user = await userManager.GetUserAsync(principal);
        if (user == null)
        {
            return TypedResults.Unauthorized();
        }

        user.Email = request.User.Email;
        user.UserName = request.User.Username;
        user.Bio = request.User.Bio;
        user.Image = request.User.Image;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return TypedResults.BadRequest(updateResult.Errors);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.GenerateToken(user, roles);
        return TypedResults.Ok(new UserResponse
        {
            User = new UserResponse.UserModel
            {
                Username = user.UserName,
                Email = user.Email,
                Token = token,
                Bio = user.Bio,
                Image = user.Image,
            }
        });  
    }
}