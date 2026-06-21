using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandeAutorisationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentsDemandesAutorisation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomFichier = table.Column<string>(type: "text", nullable: false),
                    CheminFichier = table.Column<string>(type: "text", nullable: false),
                    TypeDocument = table.Column<string>(type: "text", nullable: true),
                    DateUpload = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DemandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstSupprime = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsDemandesAutorisation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsDemandesAutorisation_DemandesAutorisation_DemandeId",
                        column: x => x.DemandeId,
                        principalTable: "DemandesAutorisation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsDemandesAutorisation_DemandeId",
                table: "DocumentsDemandesAutorisation",
                column: "DemandeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentsDemandesAutorisation");
        }
    }
}
