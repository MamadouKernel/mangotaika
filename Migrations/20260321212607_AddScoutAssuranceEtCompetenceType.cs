using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutAssuranceEtCompetenceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseGeographique",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AssuranceAnnuelle",
                table: "Scouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Competences",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseGeographique",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "AssuranceAnnuelle",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Competences");
        }
    }
}
