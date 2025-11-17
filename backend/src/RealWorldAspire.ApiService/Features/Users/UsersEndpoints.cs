namespace RealWorldAspire.ApiService.Features.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var usersEndPoints = endpoints.MapGroup("/users")
            .WithTags("Users")
            .WithOpenApi()
        ;

        usersEndPoints
            .MapPost("", UsersHandlers.Create)
            .WithName("RegisterUser")
            .WithSummary("Register a new user")
            .AllowAnonymous()
        ;

        usersEndPoints
            .MapPost("/login", UsersHandlers.Login)
            .WithName("LoginUser")
            .WithSummary("Login existing user")
            .AllowAnonymous()
        ;

        return endpoints;
    }
}
