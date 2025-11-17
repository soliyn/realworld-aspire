using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(x => x.ArticleId);

        builder.Property(e => e.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.HasMany(e => e.FavoritedByUsers)
            .WithMany(e => e.FavoritedArticles)
            .UsingEntity<FavoriteArticle>();

        builder.HasMany(e => e.Tags)
            .WithMany(e => e.Articles);

        builder.HasOne(e => e.Author)
            .WithMany(e => e.Articles);
    }
}
