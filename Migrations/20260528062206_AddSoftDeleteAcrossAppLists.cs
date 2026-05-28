using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAcrossAppLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipationsFormationRessources_RessourceId_FormationId",
                table: "ParticipationsFormationRessources");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite");

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "SessionsFormation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "RapportsActivitePiecesJointes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "QuestionsQuiz",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "PropositionsMaitriseMembres",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "PropositionsMaitriseAnnuelles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ProgrammesAnnuelsActivites",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ParticipationsFormationRessources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ParticipantsActivite",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "ModulesFormation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "MembresHistoriquesCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "LiensReseauxSociaux",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "Lecons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "JalonsFormation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "DocumentsActivite",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "AnnoncesFormation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationsFormationRessources_RessourceId_FormationId",
                table: "ParticipationsFormationRessources",
                columns: new[] { "RessourceId", "FormationId" },
                unique: true,
                filter: "\"EstSupprime\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_RessourceId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "RessourceId" },
                unique: true,
                filter: "\"RessourceId\" IS NOT NULL AND \"EstSupprime\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite",
                columns: new[] { "ActiviteId", "ScoutId" },
                unique: true,
                filter: "\"ScoutId\" IS NOT NULL AND \"EstSupprime\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipationsFormationRessources_RessourceId_FormationId",
                table: "ParticipationsFormationRessources");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_RessourceId",
                table: "ParticipantsActivite");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantsActivite_ActiviteId_ScoutId",
                table: "ParticipantsActivite");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "SessionsFormation");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "RapportsActivitePiecesJointes");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "QuestionsQuiz");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "PropositionsMaitriseMembres");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "PropositionsMaitriseAnnuelles");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ProgrammesAnnuelsActivites");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ParticipationsFormationRessources");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ParticipantsActivite");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "ModulesFormation");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "MembresHistoriquesCategories");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "LiensReseauxSociaux");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "Lecons");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "JalonsFormation");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "DocumentsActivite");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "AnnoncesFormation");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationsFormationRessources_RessourceId_FormationId",
                table: "ParticipationsFormationRessources",
                columns: new[] { "RessourceId", "FormationId" },
                unique: true);

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
        }
    }
}
