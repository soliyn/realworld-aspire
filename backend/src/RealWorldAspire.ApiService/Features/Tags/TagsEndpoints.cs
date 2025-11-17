using RealWorldAspire.ApiService.Features.Profiles;

namespace RealWorldAspire.ApiService.Features.Tags;

public static class TagsEndpoints
{
    public static IEndpointRouteBuilder MapTagsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tagsEndPoints = endpoints.MapGroup("/tags");

        tagsEndPoints.MapGet("", TagsHandlers.GetTags).AllowAnonymous();

        return endpoints;
    }
}