using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalMemberCategoryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembresHistoriquesCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreHistoriqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Categorie = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Periode = table.Column<string>(type: "text", nullable: true),
                    Ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembresHistoriquesCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembresHistoriquesCategories_MembresHistoriques_MembreHisto~",
                        column: x => x.MembreHistoriqueId,
                        principalTable: "MembresHistoriques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "MembresHistoriquesCategories" ("Id", "MembreHistoriqueId", "Categorie", "Description", "Periode", "Ordre")
                SELECT md5("Id"::text || '-1')::uuid, "Id", 1, "Description", "Periode", "Ordre"
                FROM "MembresHistoriques"
                WHERE ("Categorie" & 1) = 1;

                INSERT INTO "MembresHistoriquesCategories" ("Id", "MembreHistoriqueId", "Categorie", "Description", "Periode", "Ordre")
                SELECT md5("Id"::text || '-2')::uuid, "Id", 2, "Description", "Periode", "Ordre"
                FROM "MembresHistoriques"
                WHERE ("Categorie" & 2) = 2;

                INSERT INTO "MembresHistoriquesCategories" ("Id", "MembreHistoriqueId", "Categorie", "Description", "Periode", "Ordre")
                SELECT md5("Id"::text || '-4')::uuid, "Id", 4, "Description", "Periode", "Ordre"
                FROM "MembresHistoriques"
                WHERE ("Categorie" & 4) = 4;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MembresHistoriquesCategories_MembreHistoriqueId_Categorie",
                table: "MembresHistoriquesCategories",
                columns: new[] { "MembreHistoriqueId", "Categorie" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembresHistoriquesCategories");
        }
    }
}
