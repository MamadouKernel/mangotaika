using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class RenameGroupeNomAdjointsToNomChefGroupe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NomAdjoints",
                table: "Groupes",
                newName: "NomChefGroupe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NomChefGroupe",
                table: "Groupes",
                newName: "NomAdjoints");
        }
    }
}
