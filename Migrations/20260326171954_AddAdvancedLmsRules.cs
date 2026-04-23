using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedLmsRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateFermetureDisponibilite",
                table: "Quizzes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOuvertureDisponibilite",
                table: "Quizzes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NombreTentativesMax",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormationsPrerequis",
                columns: table => new
                {
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisFormationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormationsPrerequis", x => new { x.FormationId, x.PrerequisFormationId });
                    table.ForeignKey(
                        name: "FK_FormationsPrerequis_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormationsPrerequis_Formations_PrerequisFormationId",
                        column: x => x.PrerequisFormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JalonsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DateJalon = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    EstPublie = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JalonsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JalonsFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormationsPrerequis_PrerequisFormationId",
                table: "FormationsPrerequis",
                column: "PrerequisFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_JalonsFormation_FormationId_DateJalon",
                table: "JalonsFormation",
                columns: new[] { "FormationId", "DateJalon" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormationsPrerequis");

            migrationBuilder.DropTable(
                name: "JalonsFormation");

            migrationBuilder.DropColumn(
                name: "DateFermetureDisponibilite",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "DateOuvertureDisponibilite",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "NombreTentativesMax",
                table: "Quizzes");
        }
    }
}
