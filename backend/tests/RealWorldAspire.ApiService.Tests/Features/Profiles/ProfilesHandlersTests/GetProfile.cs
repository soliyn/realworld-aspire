using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Moq;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Profiles;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Tests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Profiles.ProfilesHandlersTests;

[Collection(nameof(PostgresDatabaseCollection))]
public class GetProfile : HandlerTestBase
{
    private readonly DbContextOptionsBuilder<RealWorldDbContext> _optionsBuilder;

    public GetProfile(PostgresTestFixture fixture)
    {
        _optionsBuilder = new DbContextOptionsBuilder<RealWorldDbContext>();
        _optionsBuilder.UseNpgsql(fixture.Postgres.GetConnectionString());
    }

    [Fact]
    public async Task Should_Return_404NotFound()
    {
        // Arrange
        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        var principal = new ClaimsPrincipal();
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());

        // Act
        await using var dbContext = new RealWorldDbContext(_optionsBuilder.Options);
        var result = await ProfilesHandlers.GetProfile("nonexistentuser", principal, userManagerMock.Object, dbContext);

        // Assert
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>()
            .Value.ShouldBe("nonexistentuser");
    }

    [Fact]
    public async Task Should_Return_Existing_Profile()
    {
        // Arrange
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            var appUser = new AppUser
            {
                UserName = "jake",
                Email = "jake@example.com",
                Bio = "I work at statefarm",
                Image = "https://api.realworld.io/images/smiley-cyrus.jpg"
            };
            await dbContext.Users.AddAsync(appUser);
            await dbContext.SaveChangesAsync();
        }

        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());

        var principal = new ClaimsPrincipal(); // Add a dummy principal

        // Act
        IResult result;
        await using (var dbContext = new RealWorldDbContext(_optionsBuilder.Options))
        {
            result = await ProfilesHandlers.GetProfile("jake", principal, userManagerMock.Object, dbContext);
        }

        // Assert
        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ProfileResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Profile.Username.ShouldBe("jake");
        okResult.Value.Profile.Bio.ShouldBe("I work at statefarm");
        okResult.Value.Profile.Image.ShouldBe("https://api.realworld.io/images/smiley-cyrus.jpg");
        okResult.Value.Profile.Following.ShouldBeFalse();
    }
}