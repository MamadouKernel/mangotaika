using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutFieldsEtLienCotisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScoutId",
                table: "TransactionsFinancieres",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fonction",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCarte",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionScoute",
                table: "Scouts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFinancieres_ScoutId",
                table: "TransactionsFinancieres",
                column: "ScoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Scouts_NumeroCarte",
                table: "Scouts",
                column: "NumeroCarte",
                unique: true,
                filter: "\"NumeroCarte\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionsFinancieres_Scouts_ScoutId",
                table: "TransactionsFinancieres",
                column: "ScoutId",
                principalTable: "Scouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionsFinancieres_Scouts_ScoutId",
                table: "TransactionsFinancieres");

            migrationBuilder.DropIndex(
                name: "IX_TransactionsFinancieres_ScoutId",
                table: "TransactionsFinancieres");

            migrationBuilder.DropIndex(
                name: "IX_Scouts_NumeroCarte",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "ScoutId",
                table: "TransactionsFinancieres");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "Fonction",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "NumeroCarte",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "RegionScoute",
                table: "Scouts");
        }
    }
}
