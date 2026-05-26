using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletToWalletTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransfertId",
                table: "MouvementsPortefeuilles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsPortefeuilles_TransfertId",
                table: "MouvementsPortefeuilles",
                column: "TransfertId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MouvementsPortefeuilles_TransfertId",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "TransfertId",
                table: "MouvementsPortefeuilles");
        }
    }
}
