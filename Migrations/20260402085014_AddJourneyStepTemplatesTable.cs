using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyStepTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelesEtapesParcours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrancheId = table.Column<Guid>(type: "uuid", nullable: true),
                    NomEtape = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CodeEtape = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false),
                    EstObligatoire = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelesEtapesParcours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelesEtapesParcours_Branches_BrancheId",
                        column: x => x.BrancheId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModeleEtapeParcours_Branche_Code_Actif",
                table: "ModelesEtapesParcours",
                columns: new[] { "BrancheId", "CodeEtape", "IsActive" },
                filter: "\"CodeEtape\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModeleEtapeParcours_Branche_Ordre_Actif",
                table: "ModelesEtapesParcours",
                columns: new[] { "BrancheId", "OrdreAffichage", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelesEtapesParcours");
        }
    }
}
