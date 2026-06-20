using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddChefGroupeRequestApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChefGroupeValidee",
                table: "DemandesAutorisation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValidationChefGroupe",
                table: "DemandesAutorisation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValideurChefGroupeId",
                table: "DemandesAutorisation",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "DemandesAutorisation" d
                SET "ChefGroupeValidee" = TRUE,
                    "DateValidationChefGroupe" = COALESCE((
                        SELECT MAX(s."Date")
                        FROM "SuivisDemande" s
                        WHERE s."DemandeId" = d."Id"
                          AND (
                              LOWER(s."Commentaire") LIKE '%validee par le chef de groupe%'
                              OR LOWER(s."Commentaire") LIKE '%valide par le chef de groupe%'
                          )
                    ), d."DateValidation")
                WHERE EXISTS (
                    SELECT 1
                    FROM "SuivisDemande" s
                    WHERE s."DemandeId" = d."Id"
                      AND (
                          LOWER(s."Commentaire") LIKE '%validee par le chef de groupe%'
                          OR LOWER(s."Commentaire") LIKE '%valide par le chef de groupe%'
                      )
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DemandesAutorisation_ValideurChefGroupeId",
                table: "DemandesAutorisation",
                column: "ValideurChefGroupeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandesAutorisation_AspNetUsers_ValideurChefGroupeId",
                table: "DemandesAutorisation",
                column: "ValideurChefGroupeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandesAutorisation_AspNetUsers_ValideurChefGroupeId",
                table: "DemandesAutorisation");

            migrationBuilder.DropIndex(
                name: "IX_DemandesAutorisation_ValideurChefGroupeId",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "ChefGroupeValidee",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "DateValidationChefGroupe",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "ValideurChefGroupeId",
                table: "DemandesAutorisation");
        }
    }
}
