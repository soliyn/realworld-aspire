namespace RealWorldAspire.ApiService.Data.Models;

public class Tag
{
    public int TagId { get; set; }
    public required string Name { get; set; }
    public List<Article> Articles { get; set; } = [];
}