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

        builder.ApplyConfigurationsFromAssembly(typeof(RealWorldDbContext).Assembly);
    }

    public virtual DbSet<Article> Articles { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }
    public virtual DbSet<FavoriteArticle> FavoriteArticles { get; set; }
    public virtual DbSet<Tag> Tags { get; set; }
    public virtual DbSet<Comment> Comments { get; set; }
}