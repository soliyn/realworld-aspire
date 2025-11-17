namespace RealWorldAspire.ApiService.Features.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var usersEndPoints = endpoints.MapGroup("/users");

        usersEndPoints.MapPost("", UsersHandlers.Create).AllowAnonymous();
        usersEndPoints.MapPost("/login", UsersHandlers.Login).AllowAnonymous();

        return endpoints;
    }
}
