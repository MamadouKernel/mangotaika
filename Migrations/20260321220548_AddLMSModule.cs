using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddLMSModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Formations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Niveau = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DureeEstimeeHeures = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DatePublication = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BrancheCibleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompetenceLieeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuteurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Formations_AspNetUsers_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Formations_Branches_BrancheCibleId",
                        column: x => x.BrancheCibleId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InscriptionsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateInscription = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateTerminee = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProgressionPourcent = table.Column<int>(type: "integer", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscriptionsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscriptionsFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InscriptionsFormation_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModulesFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulesFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulesFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lecons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContenuTexte = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    DocumentUrl = table.Column<string>(type: "text", nullable: true),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    DureeMinutes = table.Column<int>(type: "integer", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lecons_ModulesFormation_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "ModulesFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    NoteMinimale = table.Column<int>(type: "integer", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quizzes_ModulesFormation_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "ModulesFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionsLecon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstTerminee = table.Column<bool>(type: "boolean", nullable: false),
                    DateTerminee = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeconId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionsLecon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressionsLecon_Lecons_LeconId",
                        column: x => x.LeconId,
                        principalTable: "Lecons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionsLecon_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionsQuiz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enonce = table.Column<string>(type: "text", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionsQuiz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionsQuiz_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TentativesQuiz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Reussi = table.Column<bool>(type: "boolean", nullable: false),
                    DateTentative = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReponsesJson = table.Column<string>(type: "text", nullable: true),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TentativesQuiz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TentativesQuiz_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TentativesQuiz_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReponsesQuiz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Texte = table.Column<string>(type: "text", nullable: false),
                    EstCorrecte = table.Column<bool>(type: "boolean", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReponsesQuiz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReponsesQuiz_QuestionsQuiz_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionsQuiz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Formations_AuteurId",
                table: "Formations",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Formations_BrancheCibleId",
                table: "Formations",
                column: "BrancheCibleId");

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsFormation_FormationId",
                table: "InscriptionsFormation",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_InscriptionsFormation_ScoutId_FormationId",
                table: "InscriptionsFormation",
                columns: new[] { "ScoutId", "FormationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecons_ModuleId",
                table: "Lecons",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModulesFormation_FormationId",
                table: "ModulesFormation",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionsLecon_LeconId",
                table: "ProgressionsLecon",
                column: "LeconId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionsLecon_ScoutId_LeconId",
                table: "ProgressionsLecon",
                columns: new[] { "ScoutId", "LeconId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsQuiz_QuizId",
                table: "QuestionsQuiz",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes",
                column: "ModuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesQuiz_QuestionId",
                table: "ReponsesQuiz",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TentativesQuiz_QuizId",
                table: "TentativesQuiz",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_TentativesQuiz_ScoutId",
                table: "TentativesQuiz",
                column: "ScoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InscriptionsFormation");

            migrationBuilder.DropTable(
                name: "ProgressionsLecon");

            migrationBuilder.DropTable(
                name: "ReponsesQuiz");

            migrationBuilder.DropTable(
                name: "TentativesQuiz");

            migrationBuilder.DropTable(
                name: "Lecons");

            migrationBuilder.DropTable(
                name: "QuestionsQuiz");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "ModulesFormation");

            migrationBuilder.DropTable(
                name: "Formations");
        }
    }
}
