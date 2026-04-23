using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceCoreModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InscriptionsAnnuellesScouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeReference = table.Column<int>(type: "integer", nullable: false),
                    LibelleAnnee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DateInscription = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    InscriptionParoissialeValidee = table.Column<bool>(type: "boolean", nullable: false),
                    CotisationNationaleAjour = table.Column<bool>(type: "boolean", nullable: false),
                    Observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValideParId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscriptionsAnnuellesScouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscriptionsAnnuellesScouts_AspNetUsers_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InscriptionsAnnuellesScouts_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammesAnnuels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnneeReference = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Objectifs = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CalendrierSynthese = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    Observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CommentaireValidation = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateSoumission = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValideurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammesAnnuels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammesAnnuels_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammesAnnuels_AspNetUsers_ValideurId",
                        column: x => x.ValideurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProgrammesAnnuels_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropositionsMaitriseAnnuelles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeReference = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CompositionProposee = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    ObjectifsPedagogiques = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    BesoinsFormation = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CommentaireValidation = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateSoumission = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValideurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionsMaitriseAnnuelles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropositionsMaitriseAnnuelles_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropositionsMaitriseAnnuelles_AspNetUsers_ValideurId",
                        column: x => x.ValideurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PropositionsMaitriseAnnuelles_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RapportsActivite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResumeExecutif = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResultatsObtenus = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DifficultesRencontrees = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Recommandations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ObservationsComplementaires = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CommentaireValidation = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateSoumission = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValideurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportsActivite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RapportsActivite_Activites_ActiviteId",
                        column: x => x.ActiviteId,
                        principalTable: "Activites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RapportsActivite_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RapportsActivite_AspNetUsers_ValideurId",
                        column: x => x.ValideurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsAnnuellesScouts_ScoutId_AnneeReference",
                table: "InscriptionsAnnuellesScouts",
                columns: new[] { "ScoutId", "AnneeReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsAnnuellesScouts_ValideParId",
                table: "InscriptionsAnnuellesScouts",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammesAnnuels_CreateurId",
                table: "ProgrammesAnnuels",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammesAnnuels_GroupeId_AnneeReference",
                table: "ProgrammesAnnuels",
                columns: new[] { "GroupeId", "AnneeReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammesAnnuels_ValideurId",
                table: "ProgrammesAnnuels",
                column: "ValideurId");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionsMaitriseAnnuelles_CreateurId",
                table: "PropositionsMaitriseAnnuelles",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionsMaitriseAnnuelles_GroupeId_AnneeReference",
                table: "PropositionsMaitriseAnnuelles",
                columns: new[] { "GroupeId", "AnneeReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropositionsMaitriseAnnuelles_ValideurId",
                table: "PropositionsMaitriseAnnuelles",
                column: "ValideurId");

            migrationBuilder.CreateIndex(
                name: "IX_RapportsActivite_ActiviteId",
                table: "RapportsActivite",
                column: "ActiviteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RapportsActivite_CreateurId",
                table: "RapportsActivite",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_RapportsActivite_ValideurId",
                table: "RapportsActivite",
                column: "ValideurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropTable(
                name: "ProgrammesAnnuels");

            migrationBuilder.DropTable(
                name: "PropositionsMaitriseAnnuelles");

            migrationBuilder.DropTable(
                name: "RapportsActivite");
        }
    }
}
