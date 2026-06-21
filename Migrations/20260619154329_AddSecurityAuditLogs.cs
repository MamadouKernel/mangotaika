using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: true),
                    UtilisateurCibleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AncienneValeur = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    NouvelleValeur = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Commentaire = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    AdresseIp = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAuditLogs_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityAuditLogs_AspNetUsers_UtilisateurCibleId",
                        column: x => x.UtilisateurCibleId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_AuteurId",
                table: "SecurityAuditLogs",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UtilisateurCibleId_DateCreation",
                table: "SecurityAuditLogs",
                columns: new[] { "UtilisateurCibleId", "DateCreation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");
        }
    }
}
