using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandeAutorisationSoftDeleteAndNewRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateSuppression",
                table: "DemandesAutorisation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "DemandesAutorisation",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateSuppression",
                table: "DemandesAutorisation");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "DemandesAutorisation");
        }
    }
}
