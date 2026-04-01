using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingFeedbackScoutAndBranchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactUrgenceNom",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactUrgenceRelation",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactUrgenceTelephone",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FonctionVieActive",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NiveauFormationScoute",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoulardUrl",
                table: "Branches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactUrgenceNom",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "ContactUrgenceRelation",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "ContactUrgenceTelephone",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "FonctionVieActive",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "NiveauFormationScoute",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "FoulardUrl",
                table: "Branches");
        }
    }
}
