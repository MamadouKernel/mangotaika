using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationCodeSecurityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "CodesInvitation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstActif",
                table: "CodesInvitation",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleCible",
                table: "CodesInvitation",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Gestionnaire");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "CodesInvitation");

            migrationBuilder.DropColumn(
                name: "EstActif",
                table: "CodesInvitation");

            migrationBuilder.DropColumn(
                name: "RoleCible",
                table: "CodesInvitation");
        }
    }
}
