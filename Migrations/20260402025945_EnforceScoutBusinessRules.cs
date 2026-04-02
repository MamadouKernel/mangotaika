using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class EnforceScoutBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scouts_Matricule",
                table: "Scouts");

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Scouts",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.CreateIndex(
                name: "IX_Scouts_Matricule",
                table: "Scouts",
                column: "Matricule",
                unique: true,
                filter: "\"Matricule\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scouts_Matricule",
                table: "Scouts");

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Scouts",
                type: "citext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scouts_Matricule",
                table: "Scouts",
                column: "Matricule",
                unique: true);
        }
    }
}
