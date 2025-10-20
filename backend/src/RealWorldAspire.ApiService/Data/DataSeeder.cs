using Microsoft.EntityFrameworkCore;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Data;

public static class DataSeeder
{
    public static void Seed(this DbContext context)
    {
        var article = context.Set<Article>().FirstOrDefault(x => x.Slug == "how-to-learn-javascript-efficiently");
        if (article == null)
        {
            context.Set<Article>().Add(
                new Article()
                {
                    Slug = "how-to-learn-javascript-efficiently",
                    Title = "How to Learn JavaScript Efficiently",
                    Description = "A comprehensive guide to mastering JavaScript from beginner to advanced level",
                    Body = "Learning JavaScript can be overwhelming with so many resources available. Here's a structured approach that has helped thousands of developers master this essential language.\n\n## Start with the Fundamentals\n\nBefore diving into frameworks, master the core concepts: variables, functions, objects, and arrays. Understanding these building blocks is crucial for writing clean, maintainable code.\n\n## Practice with Real Projects\n\nThe best way to learn is by building actual applications. Start with simple projects like a todo list or calculator, then gradually increase complexity.\n\n## Join the Community\n\nEngage with other developers through forums, Discord servers, and local meetups. The JavaScript community is incredibly welcoming and helpful.",
                    TagList = ["beginners", "javascript", "programming", "webdev"],
                    CreatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    Author = new Author()
                    {
                        Username = "johndoe",
                        Bio = "Full-stack developer passionate about clean code and innovative solutions. Love working with modern web technologies.",
                        Image = "https://raw.githubusercontent.com/gothinkster/node-express-realworld-example-app/refs/heads/master/src/assets/images/smiley-cyrus.jpeg",
                        Following = false
                    }
                }
            );
            context.SaveChanges();
        }
    }
}