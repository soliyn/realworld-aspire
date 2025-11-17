using RealWorldAspire.ApiService.Features.Profiles;

namespace RealWorldAspire.ApiService.Features.Tags;

public static class TagsEndpoints
{
    public static IEndpointRouteBuilder MapTagsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tagsEndPoints = endpoints.MapGroup("/tags")
            .WithTags("Tags")
            .WithOpenApi()
        ;

        tagsEndPoints
            .MapGet("", TagsHandlers.GetTags)
            .WithName("GetTags")
            .WithSummary("Get all tags")
            .AllowAnonymous()
        ;

        return endpoints;
    }
}