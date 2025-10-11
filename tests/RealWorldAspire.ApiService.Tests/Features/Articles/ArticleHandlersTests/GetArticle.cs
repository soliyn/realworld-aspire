using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using RealWorldAspire.ApiService.Data;
using RealWorldAspire.ApiService.Data.Models;
using RealWorldAspire.ApiService.Features.Articles;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

public class GetArticle
{
    private readonly Mock<RealWorldDbContext> _dbContextMock;

    public GetArticle()
    {
        Mock<DbSet<Article>> articleDbSetMock = GetFakeArticles().BuildMockDbSet();
        _dbContextMock = new Mock<RealWorldDbContext>();
        _dbContextMock.Setup(x => x.Articles).Returns(articleDbSetMock.Object);
    }

    [Fact]
    public async Task Should_Return_Article()
    {
        var result = await ArticleHandlers.GetArticle("how-to-learn-javascript-efficiently", _dbContextMock.Object);

        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse()
            {
                Slug = "how-to-learn-javascript-efficiently",
                Title = "How to Learn JavaScript Efficiently",
                Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                TagList = ["beginners", "javascript", "programming", "webdev"],
                CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                Favorited = false,
                FavoritesCount = 2,
                Author = new GetArticleResponse.AuthorDto
                {
                    Username = "johndoe",
                    Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                    Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                    Following = false
                }
            });
    }

    [Fact]
    public async Task Should_Return_404NotFound()
    {
        var result = await ArticleHandlers.GetArticle("non-existent-slug", _dbContextMock.Object);
        result.ShouldBeOfType<NotFound>();
    }

    private List<Article> GetFakeArticles()
    {
        return
        [
            new Article()
            {
                Slug = "how-to-learn-javascript-efficiently",
                Title = "How to Learn JavaScript Efficiently",
                Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                TagList = ["beginners", "javascript", "programming", "webdev"],
                CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                FavoritesCount = 2,
                Author = new Author()
                {
                    Username = "johndoe",
                    Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                    Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                    Following = false
                }
            },
            new Article()
            {
                Slug = "advanced-csharp-tips",
                Title = "Advanced C# Tips",
                Description = "Take your C# skills to the next level with these advanced tips and tricks.",
                Body = "C# offers many advanced features for experienced developers.\n\n## Use LINQ Effectively\n\nLINQ can simplify complex data queries.\n\n## Master Async Programming\n\nAsync/await helps you write scalable applications.\n\n## Explore Span<T>\n\nSpan<T> enables high-performance memory access.",
                TagList = ["csharp", "dotnet", "advanced", "tips"],
                CreatedAt = new DateTime(2025, 10, 10, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 10, 12, 30, 0, DateTimeKind.Utc),
                FavoritesCount = 5,
                Author = new Author()
                {
                    Username = "janedoe",
                    Bio = "Senior .NET developer and software architect. Passionate about teaching and sharing knowledge.",
                    Image = "https://randomuser.me/api/portraits/women/44.jpg",
                    Following = false
                }
            }
        ];
    }
}