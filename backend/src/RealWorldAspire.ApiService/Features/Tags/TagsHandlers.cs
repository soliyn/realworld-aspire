using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;

namespace RealWorldAspire.ApiService.Features.Tags;

public static class TagsHandlers
{
    public static async Task<IResult> GetTags(
        RealWorldDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        var tags = await dbContext.Tags.Select(x => x.Name).ToListAsync(cancellationToken);

        return TypedResults.Ok(new TagResponse()
        {
            Tags = tags,
        });
    }

}
