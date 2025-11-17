namespace RealWorldAspire.ApiService.Features.User;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var userEndPoints = endpoints.MapGroup("/user")
            .WithTags("User");

        userEndPoints
            .MapGet("", UserHandlers.Get)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user")
            .RequireAuthorization()
        ;

        userEndPoints
            .MapPut("", UserHandlers.Update)
            .WithName("UpdateCurrentUser")
            .WithSummary("Update current authenticated user")
            .RequireAuthorization()
        ;

        return endpoints;
    }
}
