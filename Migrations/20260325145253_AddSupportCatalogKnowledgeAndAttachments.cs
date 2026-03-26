using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportCatalogKnowledgeAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCatalogueId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportCatalogueServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeParDefaut = table.Column<int>(type: "integer", nullable: false),
                    CategorieParDefaut = table.Column<int>(type: "integer", nullable: false),
                    ImpactParDefaut = table.Column<int>(type: "integer", nullable: false),
                    UrgenceParDefaut = table.Column<int>(type: "integer", nullable: false),
                    DelaiSlaHeures = table.Column<int>(type: "integer", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCatalogueServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportCatalogueServices_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SupportKnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Resume = table.Column<string>(type: "text", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    Categorie = table.Column<string>(type: "text", nullable: false),
                    MotsCles = table.Column<string>(type: "text", nullable: true),
                    EstPublie = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportKnowledgeArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportKnowledgeArticles_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketPiecesJointes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomOriginal = table.Column<string>(type: "text", nullable: false),
                    UrlFichier = table.Column<string>(type: "text", nullable: false),
                    TypeMime = table.Column<string>(type: "text", nullable: true),
                    TailleOctets = table.Column<long>(type: "bigint", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AjouteParId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPiecesJointes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPiecesJointes_AspNetUsers_AjouteParId",
                        column: x => x.AjouteParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketPiecesJointes_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { new Guid("91b7eb6f-c8de-4dd3-9785-f0e6f7932301"), null, "AgentSupport", "AGENTSUPPORT" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceCatalogueId",
                table: "Tickets",
                column: "ServiceCatalogueId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCatalogueServices_AuteurId",
                table: "SupportCatalogueServices",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCatalogueServices_Code",
                table: "SupportCatalogueServices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportKnowledgeArticles_AuteurId",
                table: "SupportKnowledgeArticles",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPiecesJointes_AjouteParId",
                table: "TicketPiecesJointes",
                column: "AjouteParId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPiecesJointes_TicketId",
                table: "TicketPiecesJointes",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_SupportCatalogueServices_ServiceCatalogueId",
                table: "Tickets",
                column: "ServiceCatalogueId",
                principalTable: "SupportCatalogueServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_SupportCatalogueServices_ServiceCatalogueId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "SupportCatalogueServices");

            migrationBuilder.DropTable(
                name: "SupportKnowledgeArticles");

            migrationBuilder.DropTable(
                name: "TicketPiecesJointes");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceCatalogueId",
                table: "Tickets");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("91b7eb6f-c8de-4dd3-9785-f0e6f7932301"));

            migrationBuilder.DropColumn(
                name: "ServiceCatalogueId",
                table: "Tickets");
        }
    }
}
