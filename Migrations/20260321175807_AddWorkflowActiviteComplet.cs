using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowActiviteComplet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPrevisionnel",
                table: "Activites",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifRejet",
                table: "Activites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomResponsable",
                table: "Activites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Activites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CommentairesActivite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    TypeAction = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentairesActivite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentairesActivite_Activites_ActiviteId",
                        column: x => x.ActiviteId,
                        principalTable: "Activites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentairesActivite_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantsActivite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    Presence = table.Column<int>(type: "integer", nullable: false),
                    DateInscription = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantsActivite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantsActivite_Activites_ActiviteId",
                        column: x => x.ActiviteId,
                        principalTable: "Activites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantsActivite_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentairesActivite_ActiviteId",
                table: "CommentairesActivite",
                column: "ActiviteId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentairesActivite_AuteurId",
                table: "CommentairesActivite",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "ScoutId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ScoutId",
                table: "ParticipantsActivite",
                column: "ScoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentairesActivite");

            migrationBuilder.DropTable(
                name: "ParticipantsActivite");

            migrationBuilder.DropColumn(
                name: "BudgetPrevisionnel",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "MotifRejet",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "NomResponsable",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Activites");
        }
    }
}
