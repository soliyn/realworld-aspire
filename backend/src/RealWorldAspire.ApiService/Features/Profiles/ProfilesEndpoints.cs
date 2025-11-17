namespace RealWorldAspire.ApiService.Features.Profiles;

public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder MapProfilesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var profilesEndPoints = endpoints.MapGroup("/profiles")
            .WithTags("Profiles");

        profilesEndPoints
            .MapGet("{username}", ProfilesHandlers.GetProfile)
            .WithName("GetProfile")
            .WithSummary("Get a user profile by username")
            .AllowAnonymous()
        ;

        profilesEndPoints
            .MapPost("{username}/follow", ProfilesHandlers.Follow)
            .WithName("FollowUser")
            .WithSummary("Follow a user")
            .RequireAuthorization()
        ;

        profilesEndPoints
            .MapDelete("{username}/follow", ProfilesHandlers.Unfollow)
            .WithName("UnfollowUser")
            .WithSummary("Unfollow a user")
            .RequireAuthorization()
        ;

        return endpoints;
    }
}