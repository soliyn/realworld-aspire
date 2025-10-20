using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.Users;

public static class UsersHandlers
{
    public static async Task<IResult> Create(CreateUserRequest request, UserManager<AppUser> userManager, JwtTokenService tokenService)
    {
        var user = new AppUser
        {
            UserName = request.User.Username,
            Email = request.User.Email
        };
        
        var result = await userManager.CreateAsync(user, request.User.Password);

        if (!result.Succeeded)
        {
            return TypedResults.BadRequest(result.Errors);
        }
        
        var registeredUser = await userManager.FindByEmailAsync(request.User.Email);
        Debug.Assert(registeredUser != null);
        var token = tokenService.GenerateToken(registeredUser, []);
        return TypedResults.Ok(new UserResponse
        {
            User = new UserResponse.UserModel
            {
                Username = registeredUser.UserName,
                Email = registeredUser.Email,
                Token = token,
            }
        });
    }

    public static async Task<IResult> Login(LoginUserRequest request, UserManager<AppUser> userManager, JwtTokenService tokenService)
    {
        var user = await userManager.FindByEmailAsync(request.User.Email);
        
        if (user != null && await userManager.CheckPasswordAsync(user, request.User.Password))
        {
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

        return TypedResults.Unauthorized();
    }
}
