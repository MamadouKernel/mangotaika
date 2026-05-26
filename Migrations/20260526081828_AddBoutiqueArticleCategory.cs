using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddBoutiqueArticleCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                table: "ArticlesBoutique",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticlesBoutique_Categorie",
                table: "ArticlesBoutique",
                column: "Categorie");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArticlesBoutique_Categorie",
                table: "ArticlesBoutique");

            migrationBuilder.DropColumn(
                name: "Categorie",
                table: "ArticlesBoutique");
        }
    }
}
