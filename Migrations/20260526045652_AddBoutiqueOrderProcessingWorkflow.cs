using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddBoutiqueOrderProcessingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentaireTraitement",
                table: "CommandesBoutique",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTraitement",
                table: "CommandesBoutique",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroRecu",
                table: "CommandesBoutique",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecuToken",
                table: "CommandesBoutique",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TraiteParId",
                table: "CommandesBoutique",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModification",
                table: "ArticlesBoutique",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandesBoutique_RecuToken",
                table: "CommandesBoutique",
                column: "RecuToken");

            migrationBuilder.CreateIndex(
                name: "IX_CommandesBoutique_Statut_DateCreation",
                table: "CommandesBoutique",
                columns: new[] { "Statut", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_CommandesBoutique_TraiteParId",
                table: "CommandesBoutique",
                column: "TraiteParId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandesBoutique_AspNetUsers_TraiteParId",
                table: "CommandesBoutique",
                column: "TraiteParId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandesBoutique_AspNetUsers_TraiteParId",
                table: "CommandesBoutique");

            migrationBuilder.DropIndex(
                name: "IX_CommandesBoutique_RecuToken",
                table: "CommandesBoutique");

            migrationBuilder.DropIndex(
                name: "IX_CommandesBoutique_Statut_DateCreation",
                table: "CommandesBoutique");

            migrationBuilder.DropIndex(
                name: "IX_CommandesBoutique_TraiteParId",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "CommentaireTraitement",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "DateTraitement",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "NumeroRecu",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "RecuToken",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "TraiteParId",
                table: "CommandesBoutique");

            migrationBuilder.DropColumn(
                name: "DateModification",
                table: "ArticlesBoutique");
        }
    }
}
