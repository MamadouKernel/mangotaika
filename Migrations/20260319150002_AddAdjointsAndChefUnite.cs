using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjointsAndChefUnite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomAdjoints",
                table: "Groupes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomChefUnite",
                table: "Branches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomAdjoints",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NomChefUnite",
                table: "Branches");
        }
    }
}
