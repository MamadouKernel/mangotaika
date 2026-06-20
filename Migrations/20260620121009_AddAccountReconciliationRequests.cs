using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountReconciliationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandesRapprochementComptes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleDemande = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Motif = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Details = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateTraitement = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TraiteParId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesRapprochementComptes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesRapprochementComptes_AspNetUsers_TraiteParId",
                        column: x => x.TraiteParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DemandesRapprochementComptes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DemandesRapprochementComptes_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesRapprochementComptes_ScoutId",
                table: "DemandesRapprochementComptes",
                column: "ScoutId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesRapprochementComptes_Statut_DateCreation",
                table: "DemandesRapprochementComptes",
                columns: new[] { "Statut", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesRapprochementComptes_TraiteParId",
                table: "DemandesRapprochementComptes",
                column: "TraiteParId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesRapprochementComptes_UserId",
                table: "DemandesRapprochementComptes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandesRapprochementComptes");
        }
    }
}
