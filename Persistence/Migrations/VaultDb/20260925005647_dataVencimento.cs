using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class dataVencimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "3bbf1aa5-745b-4622-9383-15f8e8b9c364");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataVencimento",
                table: "Movimentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "1c9f342a-182f-4100-bcdd-3e44950c0d59", "9cc36c17-7d58-4813-9709-897e039cc690", "Administrator", "ADMINISTRATOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "1c9f342a-182f-4100-bcdd-3e44950c0d59");

            migrationBuilder.DropColumn(
                name: "DataVencimento",
                table: "Movimentos");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "3bbf1aa5-745b-4622-9383-15f8e8b9c364", "a20f0dcc-9e5a-45e2-affd-b02a892be39a", "Administrator", "ADMINISTRATOR" });
        }
    }
}
