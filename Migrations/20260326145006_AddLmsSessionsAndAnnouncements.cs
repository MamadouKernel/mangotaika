using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddLmsSessionsAndAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionFormationId",
                table: "InscriptionsFormation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnnoncesFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    EstPubliee = table.Column<bool>(type: "boolean", nullable: false),
                    DatePublication = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnoncesFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnoncesFormation_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnnoncesFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EstSelfPaced = table.Column<bool>(type: "boolean", nullable: false),
                    EstPubliee = table.Column<bool>(type: "boolean", nullable: false),
                    DateOuverture = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateFermeture = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionsFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsFormation_SessionFormationId",
                table: "InscriptionsFormation",
                column: "SessionFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnoncesFormation_AuteurId",
                table: "AnnoncesFormation",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnoncesFormation_FormationId_EstPubliee_DatePublication",
                table: "AnnoncesFormation",
                columns: new[] { "FormationId", "EstPubliee", "DatePublication" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionsFormation_FormationId_EstPubliee_DateOuverture",
                table: "SessionsFormation",
                columns: new[] { "FormationId", "EstPubliee", "DateOuverture" });

            migrationBuilder.AddForeignKey(
                name: "FK_InscriptionsFormation_SessionsFormation_SessionFormationId",
                table: "InscriptionsFormation",
                column: "SessionFormationId",
                principalTable: "SessionsFormation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InscriptionsFormation_SessionsFormation_SessionFormationId",
                table: "InscriptionsFormation");

            migrationBuilder.DropTable(
                name: "AnnoncesFormation");

            migrationBuilder.DropTable(
                name: "SessionsFormation");

            migrationBuilder.DropIndex(
                name: "IX_InscriptionsFormation_SessionFormationId",
                table: "InscriptionsFormation");

            migrationBuilder.DropColumn(
                name: "SessionFormationId",
                table: "InscriptionsFormation");
        }
    }
}
