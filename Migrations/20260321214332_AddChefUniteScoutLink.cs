using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddChefUniteScoutLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChefUniteId",
                table: "Branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_ChefUniteId",
                table: "Branches",
                column: "ChefUniteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Scouts_ChefUniteId",
                table: "Branches",
                column: "ChefUniteId",
                principalTable: "Scouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Scouts_ChefUniteId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_ChefUniteId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ChefUniteId",
                table: "Branches");
        }
    }
}
