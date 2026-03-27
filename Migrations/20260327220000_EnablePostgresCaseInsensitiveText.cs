using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    /// <inheritdoc />
    public partial class EnablePostgresCaseInsensitiveText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION normalize_text_search(value text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                AS $$
                    SELECT regexp_replace(upper(unaccent(coalesce(value, ''))), '[^[:alnum:]]+', '', 'g');
                $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SupportCatalogueServices",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Scouts",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCarte",
                table: "Scouts",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CodesInvitation",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CertificationsFormation",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Groupes");

            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Branches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "left(normalize_text_search(\"Nom\"), 256)",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Groupes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "left(normalize_text_search(\"Nom\"), 256)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches",
                columns: new[] { "GroupeId", "NomNormalise" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes",
                column: "NomNormalise",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "NomNormalise",
                table: "Groupes");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SupportCatalogueServices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "Matricule",
                table: "Scouts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCarte",
                table: "Scouts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CodesInvitation",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CertificationsFormation",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Branches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "upper(btrim(\"Nom\"))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NomNormalise",
                table: "Groupes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                computedColumnSql: "upper(btrim(\"Nom\"))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_GroupeId_NomNormalise_Actif",
                table: "Branches",
                columns: new[] { "GroupeId", "NomNormalise" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Groupes_NomNormalise_Actif",
                table: "Groupes",
                column: "NomNormalise",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS citext;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS normalize_text_search(text);");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS unaccent;");
        }
    }
}
