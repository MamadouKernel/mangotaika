using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupeAssigneTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupeAssigneId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_GroupeAssigneId",
                table: "Tickets",
                column: "GroupeAssigneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Groupes_GroupeAssigneId",
                table: "Tickets",
                column: "GroupeAssigneId",
                principalTable: "Groupes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Groupes_GroupeAssigneId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_GroupeAssigneId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "GroupeAssigneId",
                table: "Tickets");
        }
    }
}
