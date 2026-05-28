using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitesScoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrancheId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Attributs = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreateurId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstActive = table.Column<bool>(type: "boolean", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesScoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitesScoutes_AspNetUsers_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UnitesScoutes_Branches_BrancheId",
                        column: x => x.BrancheId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitesScoutes_Groupes_GroupeId",
                        column: x => x.GroupeId,
                        principalTable: "Groupes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolesUnitesScoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniteScouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesUnitesScoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolesUnitesScoutes_UnitesScoutes_UniteScouteId",
                        column: x => x.UniteScouteId,
                        principalTable: "UnitesScoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffectationsUnitesScoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniteScouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleUniteScouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateAffectation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectationsUnitesScoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffectationsUnitesScoutes_RolesUnitesScoutes_RoleUniteScout~",
                        column: x => x.RoleUniteScouteId,
                        principalTable: "RolesUnitesScoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AffectationsUnitesScoutes_Scouts_ScoutId",
                        column: x => x.ScoutId,
                        principalTable: "Scouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffectationsUnitesScoutes_UnitesScoutes_UniteScouteId",
                        column: x => x.UniteScouteId,
                        principalTable: "UnitesScoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsUnitesScoutes_RoleUniteScouteId",
                table: "AffectationsUnitesScoutes",
                column: "RoleUniteScouteId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsUnitesScoutes_ScoutId",
                table: "AffectationsUnitesScoutes",
                column: "ScoutId",
                unique: true,
                filter: "\"EstActif\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsUnitesScoutes_UniteScouteId_ScoutId",
                table: "AffectationsUnitesScoutes",
                columns: new[] { "UniteScouteId", "ScoutId" },
                unique: true,
                filter: "\"EstActif\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_RolesUnitesScoutes_UniteScouteId_Nom",
                table: "RolesUnitesScoutes",
                columns: new[] { "UniteScouteId", "Nom" },
                unique: true,
                filter: "\"EstSupprime\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UnitesScoutes_BrancheId_Nom_EstSupprime",
                table: "UnitesScoutes",
                columns: new[] { "BrancheId", "Nom", "EstSupprime" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitesScoutes_CreateurId",
                table: "UnitesScoutes",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitesScoutes_GroupeId",
                table: "UnitesScoutes",
                column: "GroupeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectationsUnitesScoutes");

            migrationBuilder.DropTable(
                name: "RolesUnitesScoutes");

            migrationBuilder.DropTable(
                name: "UnitesScoutes");
        }
    }
}
