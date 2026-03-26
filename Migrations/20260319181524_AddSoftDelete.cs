using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "MotsCommissaire",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "MembresHistoriques",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "LivreDor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ContactMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Activites",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "MotsCommissaire");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "MembresHistoriques");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "LivreDor");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Activites");
        }
    }
}
