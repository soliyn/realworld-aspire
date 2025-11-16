using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Moq;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Profiles;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Profiles.ProfilesHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class Unfollow : HandlerTestBase
{
    private readonly DbContextOptionsBuilder<RealWorldDbContext> _optionsBuilder;
    private readonly string _connectionString;

    public Unfollow(PostgresTestFixture fixture)
    {
        _connectionString = fixture.Postgres.GetConnectionString();
        _optionsBuilder = new DbContextOptionsBuilder<RealWorldDbContext>();
        _optionsBuilder.UseNpgsql(fixture.Postgres.GetConnectionString());
    }

    [Fact]
    public async Task Should_Return_404NotFound_When_Follower_Is_Null()
    {
        // Arrange
        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        var principal = new ClaimsPrincipal();
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await ScopedContext.Execute(_connectionString, async dbContext =>
            await ProfilesHandlers.Unfollow("someuser", principal, userManagerMock.Object, dbContext, default)
        );

        // Assert
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>()
            .Value.ShouldBe("someuser");
    }

    [Fact]
    public async Task Should_Return_404NotFound_When_Following_User_Does_Not_Exist()
    {
        // Arrange
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            var follower = new AppUser
            {
                UserName = "follower",
                Email = "follower@example.com"
            };
            await dbContext.Users.AddAsync(follower);
            await dbContext.SaveChangesAsync();
        }

        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );

        AppUser followerUser;
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            followerUser = await dbContext.Users.FirstAsync(u => u.UserName == "follower");
        }

        var principal = new ClaimsPrincipal();
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(followerUser);
        userManagerMock.Setup(x => x.FindByNameAsync("nonexistentuser"))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await ScopedContext.Execute(_connectionString, async dbContext =>
            await ProfilesHandlers.Unfollow("nonexistentuser", principal, userManagerMock.Object, dbContext, default)
        );        

        // Assert
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>()
            .Value.ShouldBe("nonexistentuser");
    }

    [Fact]
    public async Task Should_Return_404NotFound_When_No_Follow_Relationship_Exists()
    {
        // Arrange
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            var follower = new AppUser
            {
                UserName = "alice",
                Email = "alice@example.com"
            };
            var following = new AppUser
            {
                UserName = "bob",
                Email = "bob@example.com"
            };
            await dbContext.Users.AddRangeAsync(follower, following);
            await dbContext.SaveChangesAsync();
        }

        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );

        AppUser followerUser, followingUser;
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            followerUser = await dbContext.Users.FirstAsync(u => u.UserName == "alice");
            followingUser = await dbContext.Users.FirstAsync(u => u.UserName == "bob");
        }

        var principal = new ClaimsPrincipal();
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(followerUser);
        userManagerMock.Setup(x => x.FindByNameAsync("bob"))
            .ReturnsAsync(followingUser);

        // Act
        var result = await ScopedContext.Execute(_connectionString, async dbContext =>
            await ProfilesHandlers.Unfollow("bob", principal, userManagerMock.Object, dbContext, default)
        );        

        // Assert
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }

    [Fact]
    public async Task Should_Successfully_Unfollow_User()
    {
        var followerUser = new AppUser
        {
            UserName = "charlie",
            Email = "charlie@example.com",
            Bio = "aaa"
        };
        var followingUser = new AppUser
        {
            UserName = "dave",
            Email = "dave@example.com",
        };
        ClaimsPrincipal principal;
        using (var context = ScopedContext.Create(_connectionString))
        {
            var dbContext = context.DbContext;

            
            await context.UserManager.CreateAsync(followerUser);
            await context.UserManager.CreateAsync(followingUser);
            
            followerUser = (await context.UserManager.FindByEmailAsync(followerUser.Email))!; 
            followingUser = (await context.UserManager.FindByEmailAsync(followingUser.Email))!;
            
            var userFollow = new UserFollow
            {
                FollowerId = followerUser.Id,
                FollowingId = followingUser.Id
            };
            await dbContext.UserFollows.AddAsync(userFollow);
            await dbContext.SaveChangesAsync();
            
            principal = await context.SignInManager
                .CreateUserPrincipalAsync(followerUser);
        }

        // Act
        var result = await ScopedContext.Execute(_connectionString, async (dbContext, userManager) =>
            await ProfilesHandlers.Unfollow("dave", principal, userManager, dbContext, default)
        );        

        // Assert
        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ProfileResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Profile.ShouldBeEquivalentTo(new ProfileResponse.ProfileModel()
        {
            Username = "dave",
            Following = false
        });

        // Verify the follow relationship was removed from the database
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            var userFollow = await dbContext.UserFollows
                .FirstOrDefaultAsync(x => x.FollowerId == followerUser.Id && x.FollowingId == followingUser.Id);
            userFollow.ShouldBeNull();
        }
    }
}