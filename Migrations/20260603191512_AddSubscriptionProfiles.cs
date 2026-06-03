using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfilsAbonnements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomProfil = table.Column<string>(type: "text", nullable: false),
                    Periodicite = table.Column<int>(type: "integer", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DelaiHoldJours = table.Column<int>(type: "integer", nullable: false),
                    ComptePaiementMobileId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilsAbonnements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilsAbonnements_ComptesPaiementMobile_ComptePaiementMobi~",
                        column: x => x.ComptePaiementMobileId,
                        principalTable: "ComptesPaiementMobile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbonnementsUtilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfilAbonnementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateDernierPaiement = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonnementsUtilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbonnementsUtilisateurs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbonnementsUtilisateurs_ProfilsAbonnements_ProfilAbonnement~",
                        column: x => x.ProfilAbonnementId,
                        principalTable: "ProfilsAbonnements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbonnementsUtilisateurs_ProfilAbonnementId",
                table: "AbonnementsUtilisateurs",
                column: "ProfilAbonnementId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonnementsUtilisateurs_UserId_ProfilAbonnementId_EstSuppri~",
                table: "AbonnementsUtilisateurs",
                columns: new[] { "UserId", "ProfilAbonnementId", "EstSupprime" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfilsAbonnements_ComptePaiementMobileId",
                table: "ProfilsAbonnements",
                column: "ComptePaiementMobileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfilsAbonnements_NomProfil_EstSupprime",
                table: "ProfilsAbonnements",
                columns: new[] { "NomProfil", "EstSupprime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbonnementsUtilisateurs");

            migrationBuilder.DropTable(
                name: "ProfilsAbonnements");
        }
    }
}
