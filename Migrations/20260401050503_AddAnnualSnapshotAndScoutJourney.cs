using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualSnapshotAndScoutJourney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrancheId",
                table: "InscriptionsAnnuellesScouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FonctionSnapshot",
                table: "InscriptionsAnnuellesScouts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupeId",
                table: "InscriptionsAnnuellesScouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EtapesParcoursScouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomEtape = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CodeEtape = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DatePrevisionnelle = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EstObligatoire = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesParcoursScouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesParcoursScouts_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsAnnuellesScouts_BrancheId",
                table: "InscriptionsAnnuellesScouts",
                column: "BrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsAnnuellesScouts_GroupeId",
                table: "InscriptionsAnnuellesScouts",
                column: "GroupeId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesParcoursScouts_ScoutId",
                table: "EtapesParcoursScouts",
                column: "ScoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_InscriptionsAnnuellesScouts_Branches_BrancheId",
                table: "InscriptionsAnnuellesScouts",
                column: "BrancheId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InscriptionsAnnuellesScouts_Groupes_GroupeId",
                table: "InscriptionsAnnuellesScouts",
                column: "GroupeId",
                principalTable: "Groupes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InscriptionsAnnuellesScouts_Branches_BrancheId",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropForeignKey(
                name: "FK_InscriptionsAnnuellesScouts_Groupes_GroupeId",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropTable(
                name: "EtapesParcoursScouts");

            migrationBuilder.DropIndex(
                name: "IX_InscriptionsAnnuellesScouts_BrancheId",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropIndex(
                name: "IX_InscriptionsAnnuellesScouts_GroupeId",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropColumn(
                name: "BrancheId",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropColumn(
                name: "FonctionSnapshot",
                table: "InscriptionsAnnuellesScouts");

            migrationBuilder.DropColumn(
                name: "GroupeId",
                table: "InscriptionsAnnuellesScouts");
        }
    }
}
