using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteForBusinessLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "SuivisAcademiques",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ProgrammesAnnuels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Formations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "EtapesParcoursScouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Competences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "SuivisAcademiques");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ProgrammesAnnuels");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "EtapesParcoursScouts");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Competences");
        }
    }
}
