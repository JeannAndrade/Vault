using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class RemocaoDeUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Corretoras_User_UserId",
                table: "Corretoras");

            migrationBuilder.DropForeignKey(
                name: "FK_Emissores_User_UserId",
                table: "Emissores");

            migrationBuilder.DropForeignKey(
                name: "FK_Movimentos_User_UserId",
                table: "Movimentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Objetivos_User_UserId",
                table: "Objetivos");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_User_UserId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_TiposRenda_User_UserId",
                table: "TiposRenda");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "1c9f342a-182f-4100-bcdd-3e44950c0d59");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "3b105876-cc5f-4d73-80d8-3577c4f942e0", "7a1eb57f-a943-49a8-8d4e-f9ef5c866c8a", "Administrator", "ADMINISTRATOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "3b105876-cc5f-4d73-80d8-3577c4f942e0");

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "1c9f342a-182f-4100-bcdd-3e44950c0d59", "9cc36c17-7d58-4813-9709-897e039cc690", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.AddForeignKey(
                name: "FK_Corretoras_User_UserId",
                table: "Corretoras",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Emissores_User_UserId",
                table: "Emissores",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Movimentos_User_UserId",
                table: "Movimentos",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Objetivos_User_UserId",
                table: "Objetivos",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_User_UserId",
                table: "Produtos",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TiposRenda_User_UserId",
                table: "TiposRenda",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
