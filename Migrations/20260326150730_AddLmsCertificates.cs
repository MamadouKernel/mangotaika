using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddLmsCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DelivreAttestation",
                table: "Formations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DelivreBadge",
                table: "Formations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DelivreCertificat",
                table: "Formations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CertificationsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DateEmission = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ScoreFinal = table.Column<int>(type: "integer", nullable: false),
                    Mention = table.Column<string>(type: "text", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionFormationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificationsFormation_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificationsFormation_InscriptionsFormation_InscriptionFo~",
                        column: x => x.InscriptionFormationId,
                        principalTable: "InscriptionsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CertificationsFormation_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationsFormation_Code",
                table: "CertificationsFormation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificationsFormation_FormationId",
                table: "CertificationsFormation",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationsFormation_InscriptionFormationId",
                table: "CertificationsFormation",
                column: "InscriptionFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationsFormation_ScoutId_FormationId_Type",
                table: "CertificationsFormation",
                columns: new[] { "ScoutId", "FormationId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationsFormation");

            migrationBuilder.DropColumn(
                name: "DelivreAttestation",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "DelivreBadge",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "DelivreCertificat",
                table: "Formations");
        }
    }
}
