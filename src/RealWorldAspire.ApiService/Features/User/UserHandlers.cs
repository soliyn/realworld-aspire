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
}