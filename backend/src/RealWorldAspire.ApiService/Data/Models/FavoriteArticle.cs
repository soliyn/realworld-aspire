namespace RealWorldAspire.ApiService.Data.Models;

public class FavoriteArticle
{
    public int ArticleId { get; set; }
    public required string FavoritedByUsersId { get; set; }
}