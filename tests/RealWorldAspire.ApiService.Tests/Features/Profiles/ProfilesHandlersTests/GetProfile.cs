using System.Security.Claims;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Moq;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Profiles;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
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
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        var principal = new ClaimsPrincipal(); 
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());

        var dbContextMock = new Mock<RealWorldDbContext>();
        dbContextMock.Setup(x => x.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act
        var result = await ProfilesHandlers.GetProfile("nonexistentuser", principal, userManagerMock.Object, dbContextMock.Object);

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
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());

        var principal = new ClaimsPrincipal(); // Add a dummy principal
        var dbContextMock = new Mock<RealWorldDbContext>();
        dbContextMock.Setup(x => x.Users)
            .Returns((new List<AppUser>() {appUser}).BuildMockDbSet().Object);

        // Act
        var result = await ProfilesHandlers.GetProfile("jake", principal, userManagerMock.Object, dbContextMock.Object);

        // Assert
        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ProfileResponse>>();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.Profile.Username.ShouldBe("jake");
        okResult.Value.Profile.Bio.ShouldBe("I work at statefarm");
        okResult.Value.Profile.Image.ShouldBe("https://api.realworld.io/images/smiley-cyrus.jpg");
        okResult.Value.Profile.Following.ShouldBeFalse();
    }
}