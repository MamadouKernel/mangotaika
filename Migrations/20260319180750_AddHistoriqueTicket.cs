using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoriqueTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriquesTicket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    AncienStatut = table.Column<int>(type: "integer", nullable: false),
                    NouveauStatut = table.Column<int>(type: "integer", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: true),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    DateChangement = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriquesTicket_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoriquesTicket_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesTicket_AuteurId",
                table: "HistoriquesTicket",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesTicket_TicketId",
                table: "HistoriquesTicket",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriquesTicket");
        }
    }
}
