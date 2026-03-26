using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSuiviAcademique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuivisAcademiques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnneeScolaire = table.Column<string>(type: "text", nullable: false),
                    Etablissement = table.Column<string>(type: "text", nullable: true),
                    NiveauScolaire = table.Column<string>(type: "text", nullable: false),
                    Classe = table.Column<string>(type: "text", nullable: true),
                    MoyenneGenerale = table.Column<double>(type: "double precision", nullable: true),
                    Mention = table.Column<string>(type: "text", nullable: true),
                    Observations = table.Column<string>(type: "text", nullable: true),
                    EstRedoublant = table.Column<bool>(type: "boolean", nullable: false),
                    DateSaisie = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuivisAcademiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuivisAcademiques_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuivisAcademiques_ScoutId",
                table: "SuivisAcademiques",
                column: "ScoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuivisAcademiques");
        }
    }
}
