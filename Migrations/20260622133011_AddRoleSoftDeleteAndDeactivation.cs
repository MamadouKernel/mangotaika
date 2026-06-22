using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleSoftDeleteAndDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateSuppression",
                table: "RolesMetadonnees",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstActif",
                table: "RolesMetadonnees",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstSupprime",
                table: "RolesMetadonnees",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateSuppression",
                table: "RolesMetadonnees");

            migrationBuilder.DropColumn(
                name: "EstActif",
                table: "RolesMetadonnees");

            migrationBuilder.DropColumn(
                name: "EstSupprime",
                table: "RolesMetadonnees");
        }
    }
}
