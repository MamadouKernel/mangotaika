using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddNationalDuesImportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CotisationsNationalesImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeReference = table.Column<int>(type: "integer", nullable: false),
                    NomFichier = table.Column<string>(type: "text", nullable: false),
                    DateImport = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MontantTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    NombreAjour = table.Column<int>(type: "integer", nullable: false),
                    NombreNonAjour = table.Column<int>(type: "integer", nullable: false),
                    NombreAVerifier = table.Column<int>(type: "integer", nullable: false),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotisationsNationalesImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotisationsNationalesImports_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CotisationsNationalesImportLignes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    Matricule = table.Column<string>(type: "text", nullable: false),
                    NomImporte = table.Column<string>(type: "text", nullable: true),
                    Montant = table.Column<decimal>(type: "numeric", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Motif = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotisationsNationalesImportLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotisationsNationalesImportLignes_CotisationsNationalesImpo~",
                        column: x => x.ImportId,
                        principalTable: "CotisationsNationalesImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CotisationsNationalesImportLignes_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CotisationsNationalesImportLignes_ImportId_Matricule",
                table: "CotisationsNationalesImportLignes",
                columns: new[] { "ImportId", "Matricule" });

            migrationBuilder.CreateIndex(
                name: "IX_CotisationsNationalesImportLignes_ScoutId",
                table: "CotisationsNationalesImportLignes",
                column: "ScoutId");

            migrationBuilder.CreateIndex(
                name: "IX_CotisationsNationalesImports_AnneeReference_DateImport",
                table: "CotisationsNationalesImports",
                columns: new[] { "AnneeReference", "DateImport" });

            migrationBuilder.CreateIndex(
                name: "IX_CotisationsNationalesImports_CreateurId",
                table: "CotisationsNationalesImports",
                column: "CreateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CotisationsNationalesImportLignes");

            migrationBuilder.DropTable(
                name: "CotisationsNationalesImports");
        }
    }
}
