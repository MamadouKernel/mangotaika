using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportCatalogAutoAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneParDefautId",
                table: "SupportCatalogueServices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupeParDefautId",
                table: "SupportCatalogueServices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportCatalogueServices_AssigneParDefautId",
                table: "SupportCatalogueServices",
                column: "AssigneParDefautId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCatalogueServices_GroupeParDefautId",
                table: "SupportCatalogueServices",
                column: "GroupeParDefautId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportCatalogueServices_AspNetUsers_AssigneParDefautId",
                table: "SupportCatalogueServices",
                column: "AssigneParDefautId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupportCatalogueServices_Groupes_GroupeParDefautId",
                table: "SupportCatalogueServices",
                column: "GroupeParDefautId",
                principalTable: "Groupes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportCatalogueServices_AspNetUsers_AssigneParDefautId",
                table: "SupportCatalogueServices");

            migrationBuilder.DropForeignKey(
                name: "FK_SupportCatalogueServices_Groupes_GroupeParDefautId",
                table: "SupportCatalogueServices");

            migrationBuilder.DropIndex(
                name: "IX_SupportCatalogueServices_AssigneParDefautId",
                table: "SupportCatalogueServices");

            migrationBuilder.DropIndex(
                name: "IX_SupportCatalogueServices_GroupeParDefautId",
                table: "SupportCatalogueServices");

            migrationBuilder.DropColumn(
                name: "AssigneParDefautId",
                table: "SupportCatalogueServices");

            migrationBuilder.DropColumn(
                name: "GroupeParDefautId",
                table: "SupportCatalogueServices");
        }
    }
}
