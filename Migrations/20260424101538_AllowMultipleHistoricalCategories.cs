using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleHistoricalCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "MembresHistoriques"
                SET "Categorie" = CASE "Categorie"
                    WHEN 0 THEN 1
                    WHEN 1 THEN 2
                    WHEN 2 THEN 4
                    ELSE "Categorie"
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "MembresHistoriques"
                SET "Categorie" = CASE
                    WHEN ("Categorie" & 1) = 1 THEN 0
                    WHEN ("Categorie" & 2) = 2 THEN 1
                    WHEN ("Categorie" & 4) = 4 THEN 2
                    ELSE 0
                END;
                """);
        }
    }
}
