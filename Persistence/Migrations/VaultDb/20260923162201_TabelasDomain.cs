using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations.VaultDb
{
    /// <inheritdoc />
    public partial class TabelasDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bancos");

            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "d1dc6434-7187-4e7e-b77c-3f2ec8d9c97d");

            migrationBuilder.CreateTable(
                name: "Corretoras",
                columns: table => new
                {
                    CorretoraId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corretoras", x => x.CorretoraId);
                    table.ForeignKey(
                        name: "FK_Corretoras_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Emissores",
                columns: table => new
                {
                    EmissorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emissores", x => x.EmissorId);
                    table.ForeignKey(
                        name: "FK_Emissores_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Objetivos",
                columns: table => new
                {
                    ObjetivoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Meta = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FontePagadora = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AporteMensal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OndeAplicar = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objetivos", x => x.ObjetivoId);
                    table.ForeignKey(
                        name: "FK_Objetivos_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    ProdutoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.ProdutoId);
                    table.ForeignKey(
                        name: "FK_Produtos_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposRenda",
                columns: table => new
                {
                    TipoRendaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposRenda", x => x.TipoRendaId);
                    table.ForeignKey(
                        name: "FK_TiposRenda_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Movimentos",
                columns: table => new
                {
                    MovimentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ObjetivoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoRendaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CorretoraId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProdutoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmissorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RentabilidadeContratada = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CotacaoNaCompra = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataInvestimento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ValorAporte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EhReinvestimento = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstaAtivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ValorLiquidoAtual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimentos", x => x.MovimentoId);
                    table.ForeignKey(
                        name: "FK_Movimentos_Corretoras_CorretoraId",
                        column: x => x.CorretoraId,
                        principalTable: "Corretoras",
                        principalColumn: "CorretoraId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentos_Emissores_EmissorId",
                        column: x => x.EmissorId,
                        principalTable: "Emissores",
                        principalColumn: "EmissorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentos_Objetivos_ObjetivoId",
                        column: x => x.ObjetivoId,
                        principalTable: "Objetivos",
                        principalColumn: "ObjetivoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "ProdutoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentos_TiposRenda_TipoRendaId",
                        column: x => x.TipoRendaId,
                        principalTable: "TiposRenda",
                        principalColumn: "TipoRendaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentos_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "3bbf1aa5-745b-4622-9383-15f8e8b9c364", "a20f0dcc-9e5a-45e2-affd-b02a892be39a", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.CreateIndex(
                name: "IX_Corretoras_UserId",
                table: "Corretoras",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Emissores_UserId",
                table: "Emissores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_CorretoraId",
                table: "Movimentos",
                column: "CorretoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_EmissorId",
                table: "Movimentos",
                column: "EmissorId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_ObjetivoId",
                table: "Movimentos",
                column: "ObjetivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_ProdutoId",
                table: "Movimentos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_TipoRendaId",
                table: "Movimentos",
                column: "TipoRendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_CorretoraId",
                table: "Movimentos",
                columns: new[] { "UserId", "CorretoraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_EmissorId",
                table: "Movimentos",
                columns: new[] { "UserId", "EmissorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_ObjetivoId",
                table: "Movimentos",
                columns: new[] { "UserId", "ObjetivoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_ProdutoId",
                table: "Movimentos",
                columns: new[] { "UserId", "ProdutoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentos_UserId_TipoRendaId",
                table: "Movimentos",
                columns: new[] { "UserId", "TipoRendaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Objetivos_UserId",
                table: "Objetivos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_UserId",
                table: "Produtos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposRenda_UserId",
                table: "TiposRenda",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimentos");

            migrationBuilder.DropTable(
                name: "Corretoras");

            migrationBuilder.DropTable(
                name: "Emissores");

            migrationBuilder.DropTable(
                name: "Objetivos");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "TiposRenda");

            migrationBuilder.DeleteData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "3bbf1aa5-745b-4622-9383-15f8e8b9c364");

            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    BancoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.BancoId);
                    table.ForeignKey(
                        name: "FK_Bancos_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "d1dc6434-7187-4e7e-b77c-3f2ec8d9c97d", "6ec0894c-68e1-4016-aa38-0865881ebf87", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_UserId",
                table: "Bancos",
                column: "UserId");
        }
    }
}
