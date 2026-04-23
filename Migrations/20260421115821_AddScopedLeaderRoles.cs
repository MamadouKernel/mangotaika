using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddScopedLeaderRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoulardUrl",
                table: "Branches");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("aa000001-bbbb-cccc-dddd-000000000001"), null, "AssistantCommissaire", "ASSISTANTCOMMISSAIRE" },
                    { new Guid("aa000002-bbbb-cccc-dddd-000000000002"), null, "ChefGroupe", "CHEFGROUPE" },
                    { new Guid("aa000003-bbbb-cccc-dddd-000000000003"), null, "ChefUnite", "CHEFUNITE" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000001-bbbb-cccc-dddd-000000000001"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000002-bbbb-cccc-dddd-000000000002"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000003-bbbb-cccc-dddd-000000000003"));

            migrationBuilder.AddColumn<string>(
                name: "FoulardUrl",
                table: "Branches",
                type: "text",
                nullable: true);
        }
    }
}
