namespace RealWorldAspire.ApiService.Features.Profiles;

public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder MapProfilesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var profilesEndPoints = endpoints.MapGroup("/profiles");

        profilesEndPoints.MapGet("{username}", ProfilesHandlers.GetProfile);
        profilesEndPoints.MapPost("{username}/follow", ProfilesHandlers.Follow).RequireAuthorization();
        profilesEndPoints.MapDelete("{username}/follow", ProfilesHandlers.Unfollow).RequireAuthorization();
        
        return endpoints;
    }
}