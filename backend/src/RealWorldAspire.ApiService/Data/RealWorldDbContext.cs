using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Data;

public class RealWorldDbContext : IdentityDbContext<AppUser>
{
    public RealWorldDbContext()
    {
    }
    
    public RealWorldDbContext(DbContextOptions<RealWorldDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserFollow>()
            .HasKey(x => x.Id);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);
        
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

        builder.Entity<Article>()
            .HasMany(e => e.FavoritedByUsers)
            .WithMany(e => e.FavoritedArticles)
            .UsingEntity<FavoriteArticle>()
        ;

        builder.Entity<Article>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Articles)
        ;

        builder.Entity<Article>()
            .HasOne(e => e.Author)
            .WithMany(e => e.Articles)
        ;

        builder.Entity<Comment>()
            .HasKey(e => e.Id);
        builder.Entity<Comment>()
            .Property(e => e.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        builder.Entity<Comment>()
            .Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        builder.Entity<Comment>()
            .HasOne(e => e.Author)
            .WithMany(e => e.Comments);
        builder.Entity<Comment>()
            .HasOne(e => e.Article)
            .WithMany(e => e.Comments);
    }

    public virtual DbSet<Article> Articles { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }
    public virtual DbSet<FavoriteArticle> FavoriteArticles { get; set; }
    public virtual DbSet<Tag> Tags { get; set; }
    public virtual DbSet<Comment> Comments { get; set; }
}