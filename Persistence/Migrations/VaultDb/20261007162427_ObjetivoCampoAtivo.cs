using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class ObjetivoCampoAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "925ad760-24c9-4dbc-a456-4c4ca4521588");

            migrationBuilder.AddColumn<bool>(
                name: "EstaAtivo",
                table: "Objetivos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "80a3e753-f6be-442a-bdf1-2d192770a6c5", "f6b49189-eb91-418a-a9c7-5c1cd9378520", "Administrator", "ADMINISTRATOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "80a3e753-f6be-442a-bdf1-2d192770a6c5");

            migrationBuilder.DropColumn(
                name: "EstaAtivo",
                table: "Objetivos");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "925ad760-24c9-4dbc-a456-4c4ca4521588", "84772056-e1c5-4d7b-97ea-ffbf32959dd2", "Administrator", "ADMINISTRATOR" });
        }
    }
}
