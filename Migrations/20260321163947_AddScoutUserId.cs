using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Scouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scouts_UserId",
                table: "Scouts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scouts_AspNetUsers_UserId",
                table: "Scouts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scouts_AspNetUsers_UserId",
                table: "Scouts");

            migrationBuilder.DropIndex(
                name: "IX_Scouts_UserId",
                table: "Scouts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Scouts");
        }
    }
}
