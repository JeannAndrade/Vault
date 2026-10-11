using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class MovimentoDataAtualizacaoValorLiquido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "a6555a52-0a06-44c6-977b-3183faa171d9");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAtualizacaoValorLiquido",
                table: "Movimentos",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido",
                table: "Movimentos",
                columns: new[] { "UserId", "EstaAtivo", "DataAtualizacaoValorLiquido" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "DataAtualizacaoValorLiquido",
                table: "Movimentos");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "a6555a52-0a06-44c6-977b-3183faa171d9", "7729cb8f-9cd8-4292-8f0d-67810bd24e5c", "Administrator", "ADMINISTRATOR" });
        }
    }
}
