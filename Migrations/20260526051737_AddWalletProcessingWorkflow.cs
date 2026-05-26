using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletProcessingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroRecu",
                table: "MouvementsPortefeuilles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecuToken",
                table: "MouvementsPortefeuilles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionFinanciereId",
                table: "MouvementsPortefeuilles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsPortefeuilles_RecuToken",
                table: "MouvementsPortefeuilles",
                column: "RecuToken");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsPortefeuilles_TransactionFinanciereId",
                table: "MouvementsPortefeuilles",
                column: "TransactionFinanciereId");

            migrationBuilder.AddForeignKey(
                name: "FK_MouvementsPortefeuilles_TransactionsFinancieres_Transaction~",
                table: "MouvementsPortefeuilles",
                column: "TransactionFinanciereId",
                principalTable: "TransactionsFinancieres",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MouvementsPortefeuilles_TransactionsFinancieres_Transaction~",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsPortefeuilles_RecuToken",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsPortefeuilles_TransactionFinanciereId",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "NumeroRecu",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "RecuToken",
                table: "MouvementsPortefeuilles");

            migrationBuilder.DropColumn(
                name: "TransactionFinanciereId",
                table: "MouvementsPortefeuilles");
        }
    }
}
