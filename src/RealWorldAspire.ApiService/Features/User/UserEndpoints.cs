namespace RealWorldAspire.ApiService.Features.User;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var userEndPoints = endpoints.MapGroup("/user");

        userEndPoints.MapGet("", UserHandlers.Get)
            .RequireAuthorization();
        userEndPoints.MapPut("", UserHandlers.Update)
            .RequireAuthorization();
        
        return endpoints;
    }
}
