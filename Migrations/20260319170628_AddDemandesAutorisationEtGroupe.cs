using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandesAutorisationEtGroupe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandesAutorisation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TypeActivite = table.Column<int>(type: "integer", nullable: false),
                    DateActivite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Lieu = table.Column<string>(type: "text", nullable: true),
                    NombreParticipants = table.Column<int>(type: "integer", nullable: false),
                    Objectifs = table.Column<string>(type: "text", nullable: true),
                    MoyensLogistiques = table.Column<string>(type: "text", nullable: true),
                    Budget = table.Column<string>(type: "text", nullable: true),
                    Observations = table.Column<string>(type: "text", nullable: true),
                    TdrContenu = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    MotifRejet = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DemandeurId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValideurId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesAutorisation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesAutorisation_AspNetUsers_DemandeurId",
                        column: x => x.DemandeurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesAutorisation_AspNetUsers_ValideurId",
                        column: x => x.ValideurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesAutorisation_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DemandesGroupe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomGroupe = table.Column<string>(type: "text", nullable: false),
                    Commune = table.Column<string>(type: "text", nullable: false),
                    Quartier = table.Column<string>(type: "text", nullable: false),
                    NomResponsable = table.Column<string>(type: "text", nullable: false),
                    TelephoneResponsable = table.Column<string>(type: "text", nullable: false),
                    EmailResponsable = table.Column<string>(type: "text", nullable: true),
                    Motivation = table.Column<string>(type: "text", nullable: true),
                    NombreMembresPrevus = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    MotifRejet = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateTraitement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TraiteParId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesGroupe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesGroupe_AspNetUsers_TraiteParId",
                        column: x => x.TraiteParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuivisDemande",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AncienStatut = table.Column<int>(type: "integer", nullable: false),
                    NouveauStatut = table.Column<int>(type: "integer", nullable: false),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    Auteur = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuivisDemande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuivisDemande_DemandesAutorisation_DemandeId",
                        column: x => x.DemandeId,
                        principalTable: "DemandesAutorisation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesAutorisation_DemandeurId",
                table: "DemandesAutorisation",
                column: "DemandeurId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesAutorisation_GroupeId",
                table: "DemandesAutorisation",
                column: "GroupeId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesAutorisation_ValideurId",
                table: "DemandesAutorisation",
                column: "ValideurId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesGroupe_TraiteParId",
                table: "DemandesGroupe",
                column: "TraiteParId");

            migrationBuilder.CreateIndex(
                name: "IX_SuivisDemande_DemandeId",
                table: "SuivisDemande",
                column: "DemandeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandesGroupe");

            migrationBuilder.DropTable(
                name: "SuivisDemande");

            migrationBuilder.DropTable(
                name: "DemandesAutorisation");
        }
    }
}
