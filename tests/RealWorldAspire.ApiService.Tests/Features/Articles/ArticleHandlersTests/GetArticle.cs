using Microsoft.AspNetCore.Http.HttpResults;
using RealWorldAspire.ApiService.Features.Articles;
using Shouldly;

namespace RealWorldAspire.ApiService.Tests.Features.Articles.ArticleHandlersTests;

public class GetArticle
{
    [Fact]
    public void Should_Return_Article()
    {
        var result = ArticleHandlers.GetArticle("article_slug");
        
        result.ShouldBeOfType<Ok<GetArticleResponse>>()
            .Value.ShouldBeEquivalentTo(new GetArticleResponse()
            {
                Slug = "how-to-train-your-dragon",
                Title = "How to train your dragon",
                Description = "Ever wonder how?",
                Body = "It takes a Jacobian",
                TagList = ["dragons", "training"],
                CreatedAt = DateTime.Parse("2016-02-18T03:22:56.637Z"),
                UpdatedAt = DateTime.Parse("2016-02-18T03:48:35.824Z"),
                Favorited = false,
                FavoritesCount = 0,
                Author = new GetArticleResponse.AuthorDto
                {
                    Username = "jake",
                    Bio = "I work at statefarm",
                    Image = "https://i.stack.imgur.com/xHWG8.jpg",
                    Following = false
                }
            });
    }
}