using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class RenameUserParaOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Cotacoes",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Cotacoes_UserId_Papel_Data",
                table: "Cotacoes",
                newName: "IX_Cotacoes_OwnerId_Papel_Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Cotacoes",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Cotacoes_OwnerId_Papel_Data",
                table: "Cotacoes",
                newName: "IX_Cotacoes_UserId_Papel_Data");
        }
    }
}
