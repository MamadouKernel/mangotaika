using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class RenameAssistantCommissaireAddCommissaireDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000001-bbbb-cccc-dddd-000000000001"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "EquipeDistrict", "EQUIPEDISTRICT" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { new Guid("aa000004-bbbb-cccc-dddd-000000000004"), null, "CommissaireDistrict", "COMMISSAIREDISTRICT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000004-bbbb-cccc-dddd-000000000004"));

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aa000001-bbbb-cccc-dddd-000000000001"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "AssistantCommissaire", "ASSISTANTCOMMISSAIRE" });
        }
    }
}
