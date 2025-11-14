using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Extensions;

public static class UserManagerExtensions
{
    public static async Task<AppUser> GetUserOrThrow(this UserManager<AppUser> userManager, ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
        await userManager.GetUserAsync(principal) ?? throw new UnauthorizedAccessException();
}