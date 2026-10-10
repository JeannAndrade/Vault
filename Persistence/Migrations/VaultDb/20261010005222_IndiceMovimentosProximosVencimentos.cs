using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class IndiceMovimentosProximosVencimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "fb529524-ced6-401b-b9b5-b63c4eab46ba");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "a6555a52-0a06-44c6-977b-3183faa171d9", "7729cb8f-9cd8-4292-8f0d-67810bd24e5c", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_EstaAtivo_DataVencimento",
                table: "Movimentos",
                columns: new[] { "UserId", "EstaAtivo", "DataVencimento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimentos_UserId_EstaAtivo_DataVencimento",
                table: "Movimentos");

            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "a6555a52-0a06-44c6-977b-3183faa171d9");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "fb529524-ced6-401b-b9b5-b63c4eab46ba", "6b525842-a624-43c5-bcad-cb1b86d9fce0", "Administrator", "ADMINISTRATOR" });
        }
    }
}
