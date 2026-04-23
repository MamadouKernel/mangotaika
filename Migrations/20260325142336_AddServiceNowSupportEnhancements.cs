using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceNowSupportEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateAffectation",
                table: "Tickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLimiteSla",
                table: "Tickets",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePremiereReponse",
                table: "Tickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Impact",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NumeroTicket",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResumeResolution",
                table: "Tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Urgence",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EstNoteInterne",
                table: "MessagesTicket",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "Tickets"
                SET "NumeroTicket" = 'INC-' || to_char("DateCreation", 'YYYYMMDD') || '-' || upper(substr(replace(cast("Id" as text), '-', ''), 1, 6))
                WHERE "NumeroTicket" = '';
                """);

            migrationBuilder.Sql("""
                UPDATE "Tickets"
                SET "DateLimiteSla" =
                    CASE "Priorite"
                        WHEN 3 THEN "DateCreation" + interval '4 hour'
                        WHEN 2 THEN "DateCreation" + interval '8 hour'
                        WHEN 1 THEN "DateCreation" + interval '24 hour'
                        ELSE "DateCreation" + interval '72 hour'
                    END
                WHERE "DateLimiteSla" = timestamp '0001-01-01 00:00:00';
                """);

            migrationBuilder.Sql("""
                UPDATE "Tickets"
                SET "Impact" =
                    CASE "Priorite"
                        WHEN 3 THEN 2
                        WHEN 2 THEN 1
                        ELSE 0
                    END,
                    "Urgence" =
                    CASE "Priorite"
                        WHEN 3 THEN 3
                        WHEN 2 THEN 2
                        WHEN 1 THEN 1
                        ELSE 0
                    END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Tickets"
                SET "DateAffectation" = "DateCreation"
                WHERE "AssigneAId" IS NOT NULL AND "DateAffectation" IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Tickets"
                SET "ResumeResolution" = COALESCE("CommentaireSatisfaction", 'Resolution migree depuis l''ancien workflow.')
                WHERE "DateResolution" IS NOT NULL AND "ResumeResolution" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateAffectation",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DateLimiteSla",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DatePremiereReponse",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Impact",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "NumeroTicket",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ResumeResolution",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Urgence",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EstNoteInterne",
                table: "MessagesTicket");
        }
    }
}
