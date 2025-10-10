using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Data;

public class RealWorldDbContext : DbContext
{
    public RealWorldDbContext(DbContextOptions<RealWorldDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Author>().HasKey(x => x.AuthorId);

        builder.Entity<Article>()
            .HasKey(x => x.ArticleId);
        builder.Entity<Article>()
            .Property(e => e.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            )
        ;
        builder.Entity<Article>()
            .Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            )
        ;
    }

    public DbSet<Article> Articles { get; set; }
    public DbSet<Author> Authors { get; set; }
}