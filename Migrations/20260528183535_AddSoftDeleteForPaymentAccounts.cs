using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteForPaymentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComptesPaiementMobile_NumeroMobile_EstActif",
                table: "ComptesPaiementMobile");

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ComptesPaiementMobile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ComptesPaiementMobile_NumeroMobile_EstActif_EstSupprime",
                table: "ComptesPaiementMobile",
                columns: new[] { "NumeroMobile", "EstActif", "EstSupprime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComptesPaiementMobile_NumeroMobile_EstActif_EstSupprime",
                table: "ComptesPaiementMobile");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ComptesPaiementMobile");

            migrationBuilder.CreateIndex(
                name: "IX_ComptesPaiementMobile_NumeroMobile_EstActif",
                table: "ComptesPaiementMobile",
                columns: new[] { "NumeroMobile", "EstActif" });
        }
    }
}
