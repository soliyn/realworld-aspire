using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealWorldAspire.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FavoriteArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Favorited",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "FavoritesCount",
                table: "Articles");

            migrationBuilder.CreateTable(
                name: "FavoriteArticles",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    FavoritedByUsersId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteArticles", x => new { x.ArticleId, x.FavoritedByUsersId });
                    table.ForeignKey(
                        name: "FK_FavoriteArticles_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ArticleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteArticles_AspNetUsers_FavoritedByUsersId",
                        column: x => x.FavoritedByUsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteArticles_FavoritedByUsersId",
                table: "FavoriteArticles",
                column: "FavoritedByUsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteArticles");

            migrationBuilder.AddColumn<bool>(
                name: "Favorited",
                table: "Articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FavoritesCount",
                table: "Articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
