using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddParentUserLinkSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Parents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parents_UserId",
                table: "Parents",
                column: "UserId");

            migrationBuilder.Sql("""
                UPDATE "Parents" AS p
                SET "UserId" = u."Id"
                FROM "AspNetUsers" AS u
                INNER JOIN "AspNetUserRoles" AS ur ON ur."UserId" = u."Id"
                INNER JOIN "AspNetRoles" AS r ON r."Id" = ur."RoleId"
                WHERE p."UserId" IS NULL
                  AND r."Name" = 'Parent'
                  AND (
                        (p."Telephone" IS NOT NULL AND u."PhoneNumber" IS NOT NULL AND RIGHT(REGEXP_REPLACE(p."Telephone", '[^0-9]', '', 'g'), 10) = RIGHT(REGEXP_REPLACE(u."PhoneNumber", '[^0-9]', '', 'g'), 10))
                     OR (p."Email" IS NOT NULL AND u."Email" IS NOT NULL AND UPPER(TRIM(p."Email")) = UPPER(TRIM(u."Email")))
                  );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Parents_AspNetUsers_UserId",
                table: "Parents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parents_AspNetUsers_UserId",
                table: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_Parents_UserId",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Parents");
        }
    }
}
