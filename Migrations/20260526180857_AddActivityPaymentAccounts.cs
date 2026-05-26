using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityPaymentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiviteId",
                table: "ComptesPaiementMobile",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComptesPaiementMobile_ActiviteId",
                table: "ComptesPaiementMobile",
                column: "ActiviteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComptesPaiementMobile_Activites_ActiviteId",
                table: "ComptesPaiementMobile",
                column: "ActiviteId",
                principalTable: "Activites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComptesPaiementMobile_Activites_ActiviteId",
                table: "ComptesPaiementMobile");

            migrationBuilder.DropIndex(
                name: "IX_ComptesPaiementMobile_ActiviteId",
                table: "ComptesPaiementMobile");

            migrationBuilder.DropColumn(
                name: "ActiviteId",
                table: "ComptesPaiementMobile");
        }
    }
}
