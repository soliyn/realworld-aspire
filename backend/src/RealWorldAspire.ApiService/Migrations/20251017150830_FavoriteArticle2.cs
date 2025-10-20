using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealWorldAspire.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FavoriteArticle2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FavoriteArticles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FavoriteArticles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
