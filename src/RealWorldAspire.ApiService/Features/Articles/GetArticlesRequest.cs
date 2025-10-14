namespace RealWorldAspire.ApiService.Features.Articles;

public class GetArticlesRequest
{
    public string? Tag { get; set; }
    public string? Author { get; set; }
    public bool? Favorited { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}