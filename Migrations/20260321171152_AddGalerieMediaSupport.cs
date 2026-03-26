using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddGalerieMediaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheminImage",
                table: "Galeries",
                newName: "CheminMedia");

            migrationBuilder.AddColumn<string>(
                name: "TypeMedia",
                table: "Galeries",
                type: "text",
                nullable: false,
                defaultValue: "image");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeMedia",
                table: "Galeries");

            migrationBuilder.RenameColumn(
                name: "CheminMedia",
                table: "Galeries",
                newName: "CheminImage");
        }
    }
}
