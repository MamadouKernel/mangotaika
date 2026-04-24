using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalMemberCategoryPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "MembresHistoriquesCategories",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "MembresHistoriquesCategories" AS c
                SET "PhotoUrl" = m."PhotoUrl"
                FROM "MembresHistoriques" AS m
                WHERE c."MembreHistoriqueId" = m."Id"
                  AND c."PhotoUrl" IS NULL
                  AND m."PhotoUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "MembresHistoriquesCategories");
        }
    }
}
