using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class ImproveWalletTransfersAuditAndConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseIp",
                table: "MouvementsPortefeuilles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldeApres",
                table: "MouvementsPortefeuilles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldeAvant",
                table: "MouvementsPortefeuilles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "MouvementsPortefeuilles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseIp",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "SoldeApres",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "SoldeAvant",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "MouvementsPortefeuilles");
        }
    }
}
