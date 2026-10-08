using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class IndiceMovimentoUserDataInvestimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "80a3e753-f6be-442a-bdf1-2d192770a6c5");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "fb529524-ced6-401b-b9b5-b63c4eab46ba", "6b525842-a624-43c5-bcad-cb1b86d9fce0", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_DataInvestimento",
                table: "Movimentos",
                columns: new[] { "UserId", "DataInvestimento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimentos_UserId_DataInvestimento",
                table: "Movimentos");

            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "fb529524-ced6-401b-b9b5-b63c4eab46ba");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "80a3e753-f6be-442a-bdf1-2d192770a6c5", "f6b49189-eb91-418a-a9c7-5c1cd9378520", "Administrator", "ADMINISTRATOR" });
        }
    }
}
