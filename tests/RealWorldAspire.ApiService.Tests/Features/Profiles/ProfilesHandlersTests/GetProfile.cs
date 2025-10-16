using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Moq;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Profiles;
using Microsoft.AspNetCore.Http;
using RealWorldAspire.ApiService.Data;
using Xunit;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Profiles.ProfilesHandlersTests;

public class GetProfile
{
    [Fact]
    public async Task Should_Return_404NotFound()
    {
        // Arrange
        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null
        );
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);
        var principal = new ClaimsPrincipal(); 
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());

        var dbContext = new Mock<RealWorldDbContext>().Object;

        // Act
        var result = await ProfilesHandlers.GetProfile("nonexistentuser", principal, userManagerMock.Object, dbContext);

        // Assert
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<string>>()
            .Value.ShouldBe("nonexistentuser");
    }

    [Fact]
    public async Task Should_Return_Existing_Profile()
    {
        // Arrange
        var appUser = new AppUser
        {
            UserName = "jake",
            Bio = "I work at statefarm",
            Image = "https://api.realworld.io/images/smiley-cyrus.jpg"
        };

        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null
        );
        userManagerMock.Setup(x => x.FindByNameAsync("jake"))
            .ReturnsAsync(appUser);

        var principal = new ClaimsPrincipal(); // Add a dummy principal
        var dbContext = new Mock<RealWorldDbContext>().Object; // Add a dummy dbContext

        // Act
        var result = await ProfilesHandlers.GetProfile("jake", principal, userManagerMock.Object, dbContext);

        // Assert
        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ProfileResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Profile.Username.ShouldBe("jake");
        okResult.Value.Profile.Bio.ShouldBe("I work at statefarm");
        okResult.Value.Profile.Image.ShouldBe("https://api.realworld.io/images/smiley-cyrus.jpg");
        okResult.Value.Profile.Following.ShouldBeFalse();
    }
}