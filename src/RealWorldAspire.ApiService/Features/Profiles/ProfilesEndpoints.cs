namespace RealWorldAspire.ApiService.Features.Profiles;

public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder MapProfilesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var userEndPoints = endpoints.MapGroup("/profiles");

        userEndPoints.MapGet("{username}", ProfilesHandlers.GetProfile);
        userEndPoints.MapPost("{username}/follow", ProfilesHandlers.Follow);
        userEndPoints.MapDelete("{username}/follow", ProfilesHandlers.Unfollow);
        
        return endpoints;
    }
}