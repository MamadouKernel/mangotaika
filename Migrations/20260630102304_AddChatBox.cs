using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddChatBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisiteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateFermeture = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatConversations_AspNetUsers_AgentId",
                        column: x => x.AgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatConversations_AspNetUsers_VisiteurId",
                        column: x => x.VisiteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaqEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Reponse = table.Column<string>(type: "text", nullable: false),
                    MotsCles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Categorie = table.Column<string>(type: "text", nullable: true),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpediteurId = table.Column<Guid>(type: "uuid", nullable: true),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    EstBot = table.Column<bool>(type: "boolean", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstLu = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_ExpediteurId",
                        column: x => x.ExpediteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_AgentId",
                table: "ChatConversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_Statut_DateCreation",
                table: "ChatConversations",
                columns: new[] { "Statut", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_VisiteurId",
                table: "ChatConversations",
                column: "VisiteurId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_DateEnvoi",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "DateEnvoi" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ExpediteurId",
                table: "ChatMessages",
                column: "ExpediteurId");

            migrationBuilder.CreateIndex(
                name: "IX_FaqEntries_EstActif_Categorie",
                table: "FaqEntries",
                columns: new[] { "EstActif", "Categorie" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "FaqEntries");

            migrationBuilder.DropTable(
                name: "ChatConversations");
        }
    }
}
