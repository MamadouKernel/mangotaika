using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletDonationShopAndTerritory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DistrictScoutId",
                table: "Groupes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictScoutId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArticlesBoutique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Prix = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "text", nullable: false),
                    StockDisponible = table.Column<int>(type: "integer", nullable: false),
                    EstPublie = table.Column<bool>(type: "boolean", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticlesBoutique", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommandesBoutique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    NomClient = table.Column<string>(type: "text", nullable: false),
                    TelephoneClient = table.Column<string>(type: "text", nullable: false),
                    EmailClient = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "text", nullable: false),
                    ReferencePaiement = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandesBoutique", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandesBoutique_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComptesPaiementMobile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "text", nullable: false),
                    Operateur = table.Column<string>(type: "text", nullable: false),
                    NumeroMobile = table.Column<string>(type: "text", nullable: false),
                    NomTitulaire = table.Column<string>(type: "text", nullable: true),
                    EstPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifieParId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComptesPaiementMobile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComptesPaiementMobile_AspNetUsers_ModifieParId",
                        column: x => x.ModifieParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DonsPublics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomDonateur = table.Column<string>(type: "text", nullable: false),
                    Telephone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "text", nullable: false),
                    ReferencePaiement = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonsPublics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortefeuillesUtilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Solde = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "text", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortefeuillesUtilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortefeuillesUtilisateurs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionsScoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EstActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionsScoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LignesCommandesBoutique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandeBoutiqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleBoutiqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesCommandesBoutique", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesCommandesBoutique_ArticlesBoutique_ArticleBoutiqueId",
                        column: x => x.ArticleBoutiqueId,
                        principalTable: "ArticlesBoutique",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LignesCommandesBoutique_CommandesBoutique_CommandeBoutiqueId",
                        column: x => x.CommandeBoutiqueId,
                        principalTable: "CommandesBoutique",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MouvementsPortefeuilles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortefeuilleUtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "text", nullable: false),
                    Libelle = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValideParId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouvementsPortefeuilles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MouvementsPortefeuilles_AspNetUsers_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MouvementsPortefeuilles_PortefeuillesUtilisateurs_Portefeui~",
                        column: x => x.PortefeuilleUtilisateurId,
                        principalTable: "PortefeuillesUtilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistrictsScouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RegionScouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistrictsScouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistrictsScouts_RegionsScoutes_RegionScouteId",
                        column: x => x.RegionScouteId,
                        principalTable: "RegionsScoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("aa000005-bbbb-cccc-dddd-000000000005"), null, "CommissaireDistrictAdjoint", "COMMISSAIREDISTRICTADJOINT" },
                    { new Guid("aa000006-bbbb-cccc-dddd-000000000006"), null, "AssistantCommissaireDistrict", "ASSISTANTCOMMISSAIREDISTRICT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groupes_DistrictScoutId",
                table: "Groupes",
                column: "DistrictScoutId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DistrictScoutId",
                table: "AspNetUsers",
                column: "DistrictScoutId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticlesBoutique_EstPublie_EstSupprime",
                table: "ArticlesBoutique",
                columns: new[] { "EstPublie", "EstSupprime" });

            migrationBuilder.CreateIndex(
                name: "IX_CommandesBoutique_ClientId",
                table: "CommandesBoutique",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ComptesPaiementMobile_ModifieParId",
                table: "ComptesPaiementMobile",
                column: "ModifieParId");

            migrationBuilder.CreateIndex(
                name: "IX_ComptesPaiementMobile_NumeroMobile_EstActif",
                table: "ComptesPaiementMobile",
                columns: new[] { "NumeroMobile", "EstActif" });

            migrationBuilder.CreateIndex(
                name: "IX_DistrictsScouts_Nom",
                table: "DistrictsScouts",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_DistrictsScouts_RegionScouteId",
                table: "DistrictsScouts",
                column: "RegionScouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DonsPublics_Statut_DateCreation",
                table: "DonsPublics",
                columns: new[] { "Statut", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_LignesCommandesBoutique_ArticleBoutiqueId",
                table: "LignesCommandesBoutique",
                column: "ArticleBoutiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesCommandesBoutique_CommandeBoutiqueId",
                table: "LignesCommandesBoutique",
                column: "CommandeBoutiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsPortefeuilles_PortefeuilleUtilisateurId_DateCreat~",
                table: "MouvementsPortefeuilles",
                columns: new[] { "PortefeuilleUtilisateurId", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsPortefeuilles_ValideParId",
                table: "MouvementsPortefeuilles",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_PortefeuillesUtilisateurs_UserId",
                table: "PortefeuillesUtilisateurs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionsScoutes_Nom",
                table: "RegionsScoutes",
                column: "Nom");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_DistrictsScouts_DistrictScoutId",
                table: "AspNetUsers",
                column: "DistrictScoutId",
                principalTable: "DistrictsScouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Groupes_DistrictsScouts_DistrictScoutId",
                table: "Groupes",
                column: "DistrictScoutId",
                principalTable: "DistrictsScouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_DistrictsScouts_DistrictScoutId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Groupes_DistrictsScouts_DistrictScoutId",
                table: "Groupes");

            migrationBuilder.DropTable(
                name: "ComptesPaiementMobile");

            migrationBuilder.DropTable(
                name: "DistrictsScouts");

            migrationBuilder.DropTable(
                name: "DonsPublics");

            migrationBuilder.DropTable(
                name: "LignesCommandesBoutique");

            migrationBuilder.DropTable(
                name: "MouvementsPortefeuilles");

            migrationBuilder.DropTable(
                name: "RegionsScoutes");

            migrationBuilder.DropTable(
                name: "ArticlesBoutique");

            migrationBuilder.DropTable(
                name: "CommandesBoutique");

            migrationBuilder.DropTable(
                name: "PortefeuillesUtilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_Groupes_DistrictScoutId",
                table: "Groupes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DistrictScoutId",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000005-bbbb-cccc-dddd-000000000005"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000006-bbbb-cccc-dddd-000000000006"));

            migrationBuilder.DropColumn(
                name: "DistrictScoutId",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "DistrictScoutId",
                table: "AspNetUsers");
        }
    }
}
