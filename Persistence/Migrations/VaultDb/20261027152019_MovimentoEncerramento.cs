using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class MovimentoEncerramento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEncerramento",
                table: "Movimentos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEncerramento",
                table: "Movimentos",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE Movimentos
                SET MotivoEncerramento = 'Pendente',
                    DataEncerramento = GREATEST(DATE(DataInvestimento), LEAST(COALESCE(DATE(DataVencimento), DATE(DataAtualizacaoValorLiquido), DATE(DataInvestimento)), UTC_DATE()))
                WHERE EstaAtivo = 0;
                ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimentos_Encerramento",
                table: "Movimentos",
                sql: "((EstaAtivo = 1 AND MotivoEncerramento IS NULL AND DataEncerramento IS NULL) OR (EstaAtivo = 0 AND MotivoEncerramento IS NOT NULL AND DataEncerramento IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimentos_Encerramento",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "DataEncerramento",
                table: "Movimentos");

            migrationBuilder.DropColumn(
                name: "MotivoEncerramento",
                table: "Movimentos");
        }
    }
}
