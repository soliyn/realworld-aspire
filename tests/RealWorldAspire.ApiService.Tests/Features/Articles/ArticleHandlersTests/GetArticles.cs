using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using MockQueryable.Moq;
using Moq;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using Shouldly;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

public class GetArticles
{
    private readonly Mock<RealWorldDbContext> _dbContextMock;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly ClaimsPrincipal _principal;

    public GetArticles()
    {
        Mock<DbSet<Article>> articleDbSetMock = GetFakeArticles().BuildMockDbSet();
        _dbContextMock = new Mock<RealWorldDbContext>();
        _dbContextMock.Setup(x => x.Articles).Returns(articleDbSetMock.Object);

        _userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null
        );
        _principal = new ClaimsPrincipal(); 
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new AppUser());
    }

    [Fact]
    public async Task Should_Return_10_First_Articles()
    {
        var articleDbSetMock = GetFakeArticles().BuildMockDbSet();
        var dbContextMock = new Mock<RealWorldDbContext>();
        dbContextMock.Setup(x => x.Articles).Returns(articleDbSetMock.Object);

        // Act
        var request = new GetArticlesRequest()
        {
            Limit = 10,
        };

        // Assert
        var result = await ArticleHandlers.GetArticles(request, _principal, _userManagerMock.Object, dbContextMock.Object);
        result.ShouldBeOfType<Ok<GetArticlesResponse>>();
        var okResult = result as Ok<GetArticlesResponse>;
        okResult.ShouldNotBeNull();
        var returnedArticles = okResult.Value?.Articles;
        var countResult = okResult.Value?.ArticlesCount;

        returnedArticles.ShouldNotBeNull().Count.ShouldBe(10);
        countResult.ShouldBe(10);
    }

    private List<Article> GetFakeArticles()
    {
        var authorFaker = new Faker<Author>()
            .RuleFor(a => a.Username, f => f.Internet.UserName())
            .RuleFor(a => a.Bio, f => f.Lorem.Sentence(10))
            .RuleFor(a => a.Image, f => f.Internet.Avatar())
            .RuleFor(a => a.Following, f => f.Random.Bool());

        var articleFaker = new Faker<Article>()
            .RuleFor(a => a.Slug, f => f.Lorem.Slug())
            .RuleFor(a => a.Title, f => f.Lorem.Sentence(5, 3))
            .RuleFor(a => a.Description, f => f.Lorem.Sentence(10, 5))
            .RuleFor(a => a.Body, f => f.Lorem.Paragraphs(3))
            .RuleFor(a => a.TagList, f => Enumerable.Range(0, 4).Select(_ => f.Lorem.Word()).ToList())
            .RuleFor(a => a.CreatedAt, f => f.Date.Past(2, DateTime.UtcNow))
            .RuleFor(a => a.UpdatedAt, (f, a) => a.CreatedAt.AddMinutes(f.Random.Int(0, 1440)))
            .RuleFor(a => a.Author, _ => authorFaker.Generate());

        // Generate 20 random articles
        return articleFaker.Generate(20);
    }
}