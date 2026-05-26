using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationProcessingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentaireTraitement",
                table: "DonsPublics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTraitement",
                table: "DonsPublics",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroRecu",
                table: "DonsPublics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecuToken",
                table: "DonsPublics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TraiteParId",
                table: "DonsPublics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionFinanciereId",
                table: "DonsPublics",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonsPublics_RecuToken",
                table: "DonsPublics",
                column: "RecuToken");

            migrationBuilder.CreateIndex(
                name: "IX_DonsPublics_TraiteParId",
                table: "DonsPublics",
                column: "TraiteParId");

            migrationBuilder.CreateIndex(
                name: "IX_DonsPublics_TransactionFinanciereId",
                table: "DonsPublics",
                column: "TransactionFinanciereId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonsPublics_AspNetUsers_TraiteParId",
                table: "DonsPublics",
                column: "TraiteParId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DonsPublics_TransactionsFinancieres_TransactionFinanciereId",
                table: "DonsPublics",
                column: "TransactionFinanciereId",
                principalTable: "TransactionsFinancieres",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonsPublics_AspNetUsers_TraiteParId",
                table: "DonsPublics");

            migrationBuilder.DropForeignKey(
                name: "FK_DonsPublics_TransactionsFinancieres_TransactionFinanciereId",
                table: "DonsPublics");

            migrationBuilder.DropIndex(
                name: "IX_DonsPublics_RecuToken",
                table: "DonsPublics");

            migrationBuilder.DropIndex(
                name: "IX_DonsPublics_TraiteParId",
                table: "DonsPublics");

            migrationBuilder.DropIndex(
                name: "IX_DonsPublics_TransactionFinanciereId",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "CommentaireTraitement",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "DateTraitement",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "NumeroRecu",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "RecuToken",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "TraiteParId",
                table: "DonsPublics");

            migrationBuilder.DropColumn(
                name: "TransactionFinanciereId",
                table: "DonsPublics");
        }
    }
}
