using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredWorkflowCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateRealisation",
                table: "RapportsActivite",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NombreParticipants",
                table: "RapportsActivite",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProgrammesAnnuelsActivites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeAnnuelId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomActivite = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BrancheId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cible = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Objectif = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Lieu = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    DateActivite = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Responsable = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(2500)", maxLength: 2500, nullable: false),
                    MontantParticipation = table.Column<decimal>(type: "numeric", nullable: true),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammesAnnuelsActivites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammesAnnuelsActivites_Branches_BrancheId",
                        column: x => x.BrancheId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgrammesAnnuelsActivites_ProgrammesAnnuels_ProgrammeAnnue~",
                        column: x => x.ProgrammeAnnuelId,
                        principalTable: "ProgrammesAnnuels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropositionsMaitriseMembres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropositionMaitriseAnnuelleId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomChef = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Fonction = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BrancheId = table.Column<Guid>(type: "uuid", nullable: true),
                    Contact = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    NiveauFormation = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionsMaitriseMembres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropositionsMaitriseMembres_Branches_BrancheId",
                        column: x => x.BrancheId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PropositionsMaitriseMembres_PropositionsMaitriseAnnuelles_P~",
                        column: x => x.PropositionMaitriseAnnuelleId,
                        principalTable: "PropositionsMaitriseAnnuelles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RapportsActivitePiecesJointes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RapportActiviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomFichier = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    UrlFichier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TypeMime = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DateAjout = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportsActivitePiecesJointes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RapportsActivitePiecesJointes_RapportsActivite_RapportActiv~",
                        column: x => x.RapportActiviteId,
                        principalTable: "RapportsActivite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammesAnnuelsActivites_BrancheId",
                table: "ProgrammesAnnuelsActivites",
                column: "BrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammesAnnuelsActivites_ProgrammeAnnuelId_OrdreAffichage",
                table: "ProgrammesAnnuelsActivites",
                columns: new[] { "ProgrammeAnnuelId", "OrdreAffichage" });

            migrationBuilder.CreateIndex(
                name: "IX_PropositionsMaitriseMembres_BrancheId",
                table: "PropositionsMaitriseMembres",
                column: "BrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionsMaitriseMembres_PropositionMaitriseAnnuelleId_O~",
                table: "PropositionsMaitriseMembres",
                columns: new[] { "PropositionMaitriseAnnuelleId", "OrdreAffichage" });

            migrationBuilder.CreateIndex(
                name: "IX_RapportsActivitePiecesJointes_RapportActiviteId_DateAjout",
                table: "RapportsActivitePiecesJointes",
                columns: new[] { "RapportActiviteId", "DateAjout" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgrammesAnnuelsActivites");

            migrationBuilder.DropTable(
                name: "PropositionsMaitriseMembres");

            migrationBuilder.DropTable(
                name: "RapportsActivitePiecesJointes");

            migrationBuilder.DropColumn(
                name: "DateRealisation",
                table: "RapportsActivite");

            migrationBuilder.DropColumn(
                name: "NombreParticipants",
                table: "RapportsActivite");
        }
    }
}
