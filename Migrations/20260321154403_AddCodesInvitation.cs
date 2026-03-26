using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddCodesInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodesInvitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    EstUtilise = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateUtilisation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisePaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodesInvitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodesInvitation_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodesInvitation_AspNetUsers_UtilisePaId",
                        column: x => x.UtilisePaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodesInvitation_Code",
                table: "CodesInvitation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodesInvitation_CreateurId",
                table: "CodesInvitation",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_CodesInvitation_UtilisePaId",
                table: "CodesInvitation",
                column: "UtilisePaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodesInvitation");
        }
    }
}
