using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddRessourcesParticipation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScoutId",
                table: "ParticipantsActivite",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "RessourceId",
                table: "ParticipantsActivite",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ressources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Telephone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ressources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ressources_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationsFormationRessources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RessourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateInscription = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationsFormationRessources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipationsFormationRessources_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipationsFormationRessources_Ressources_RessourceId",
                        column: x => x.RessourceId,
                        principalTable: "Ressources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_RessourceId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "RessourceId" },
                unique: true,
                filter: "\"RessourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "ScoutId" },
                unique: true,
                filter: "\"ScoutId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_RessourceId",
                table: "ParticipantsActivite",
                column: "RessourceId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParticipantsActivite_ParticipantType",
                table: "ParticipantsActivite",
                sql: "(\"ScoutId\" IS NOT NULL AND \"RessourceId\" IS NULL) OR (\"ScoutId\" IS NULL AND \"RessourceId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationsFormationRessources_FormationId",
                table: "ParticipationsFormationRessources",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationsFormationRessources_RessourceId_FormationId",
                table: "ParticipationsFormationRessources",
                columns: new[] { "RessourceId", "FormationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressources_GroupeId_Type_Nom_Prenom",
                table: "Ressources",
                columns: new[] { "GroupeId", "Type", "Nom", "Prenom" });

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantsActivite_Ressources_RessourceId",
                table: "ParticipantsActivite",
                column: "RessourceId",
                principalTable: "Ressources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantsActivite_Ressources_RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.DropTable(
                name: "ParticipationsFormationRessources");

            migrationBuilder.DropTable(
                name: "Ressources");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParticipantsActivite_ParticipantType",
                table: "ParticipantsActivite");

            migrationBuilder.DropColumn(
                name: "RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScoutId",
                table: "ParticipantsActivite",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "ScoutId" },
                unique: true);
        }
    }
}
