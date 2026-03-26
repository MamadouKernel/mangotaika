using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddLmsForumDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscussionsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    ContenuInitial = table.Column<string>(type: "text", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateDerniereActivite = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstVerrouillee = table.Column<bool>(type: "boolean", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionsFormation_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscussionsFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagesDiscussionFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    DiscussionFormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesDiscussionFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagesDiscussionFormation_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessagesDiscussionFormation_DiscussionsFormation_Discussion~",
                        column: x => x.DiscussionFormationId,
                        principalTable: "DiscussionsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionsFormation_AuteurId",
                table: "DiscussionsFormation",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionsFormation_FormationId_DateDerniereActivite",
                table: "DiscussionsFormation",
                columns: new[] { "FormationId", "DateDerniereActivite" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagesDiscussionFormation_AuteurId",
                table: "MessagesDiscussionFormation",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagesDiscussionFormation_DiscussionFormationId_DateCreat~",
                table: "MessagesDiscussionFormation",
                columns: new[] { "DiscussionFormationId", "DateCreation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagesDiscussionFormation");

            migrationBuilder.DropTable(
                name: "DiscussionsFormation");
        }
    }
}
