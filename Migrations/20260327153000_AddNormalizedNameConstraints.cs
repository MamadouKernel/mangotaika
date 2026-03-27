using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedNameConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Branches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "upper(btrim(\"Nom\"))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Groupes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "upper(btrim(\"Nom\"))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches",
                columns: new[] { "GroupeId", "NomNormalise" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes",
                column: "NomNormalise",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Groupes");
        }
    }
}
