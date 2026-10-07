using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class MovimentoCamposAdicionais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "3b105876-cc5f-4d73-80d8-3577c4f942e0");

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "Movimentos",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Protocolo",
                table: "Movimentos",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantidade",
                table: "Movimentos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "925ad760-24c9-4dbc-a456-4c4ca4521588", "84772056-e1c5-4d7b-97ea-ffbf32959dd2", "Administrator", "ADMINISTRATOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "925ad760-24c9-4dbc-a456-4c4ca4521588");

            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "Protocolo",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "Quantidade",
                table: "Movimentos");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "3b105876-cc5f-4d73-80d8-3577c4f942e0", "7a1eb57f-a943-49a8-8d4e-f9ef5c866c8a", "Administrator", "ADMINISTRATOR" });
        }
    }
}
