using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancesAGRPartenaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiensReseauxSociaux",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plateforme = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Icone = table.Column<string>(type: "text", nullable: true),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiensReseauxSociaux", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partenaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    SiteWeb = table.Column<string>(type: "text", nullable: true),
                    TypePartenariat = table.Column<string>(type: "text", nullable: true),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partenaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjetsAGR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    BudgetInitial = table.Column<decimal>(type: "numeric", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Responsable = table.Column<string>(type: "text", nullable: true),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsAGR", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjetsAGR_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjetsAGR_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TransactionsFinancieres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "text", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Categorie = table.Column<int>(type: "integer", nullable: false),
                    DateTransaction = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiviteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjetAGRId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionsFinancieres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionsFinancieres_Activites_ActiviteId",
                        column: x => x.ActiviteId,
                        principalTable: "Activites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionsFinancieres_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionsFinancieres_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionsFinancieres_ProjetsAGR_ProjetAGRId",
                        column: x => x.ProjetAGRId,
                        principalTable: "ProjetsAGR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsAGR_CreateurId",
                table: "ProjetsAGR",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsAGR_GroupeId",
                table: "ProjetsAGR",
                column: "GroupeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFinancieres_ActiviteId",
                table: "TransactionsFinancieres",
                column: "ActiviteId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFinancieres_CreateurId",
                table: "TransactionsFinancieres",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFinancieres_GroupeId",
                table: "TransactionsFinancieres",
                column: "GroupeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFinancieres_ProjetAGRId",
                table: "TransactionsFinancieres",
                column: "ProjetAGRId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiensReseauxSociaux");

            migrationBuilder.DropTable(
                name: "Partenaires");

            migrationBuilder.DropTable(
                name: "TransactionsFinancieres");

            migrationBuilder.DropTable(
                name: "ProjetsAGR");
        }
    }
}
