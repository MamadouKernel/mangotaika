using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandeAutorisationBranchAndResponsables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrancheId",
                table: "DemandesAutorisation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Responsables",
                table: "DemandesAutorisation",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandesAutorisation_BrancheId",
                table: "DemandesAutorisation",
                column: "BrancheId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandesAutorisation_Branches_BrancheId",
                table: "DemandesAutorisation",
                column: "BrancheId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandesAutorisation_Branches_BrancheId",
                table: "DemandesAutorisation");

            migrationBuilder.DropIndex(
                name: "IX_DemandesAutorisation_BrancheId",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "BrancheId",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "Responsables",
                table: "DemandesAutorisation");
        }
    }
}
