using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LocadoraVeiculos.API.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasVeiculo",
                columns: table => new
                {
                    CategoriaVeiculoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValorDiariaBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasVeiculo", x => x.CategoriaVeiculoId);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CPF = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.ClienteId);
                });

            migrationBuilder.CreateTable(
                name: "Fabricantes",
                columns: table => new
                {
                    FabricanteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PaisOrigem = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fabricantes", x => x.FabricanteId);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    VeiculoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Modelo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    AnoFabricacao = table.Column<int>(type: "int", nullable: false),
                    Quilometragem = table.Column<int>(type: "int", nullable: false),
                    Disponivel = table.Column<bool>(type: "bit", nullable: false),
                    FabricanteId = table.Column<int>(type: "int", nullable: false),
                    CategoriaVeiculoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.VeiculoId);
                    table.ForeignKey(
                        name: "FK_Veiculos_CategoriasVeiculo_CategoriaVeiculoId",
                        column: x => x.CategoriaVeiculoId,
                        principalTable: "CategoriasVeiculo",
                        principalColumn: "CategoriaVeiculoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Veiculos_Fabricantes_FabricanteId",
                        column: x => x.FabricanteId,
                        principalTable: "Fabricantes",
                        principalColumn: "FabricanteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alugueis",
                columns: table => new
                {
                    AluguelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFimPrevista = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataDevolucao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuilometragemInicial = table.Column<int>(type: "int", nullable: false),
                    QuilometragemFinal = table.Column<int>(type: "int", nullable: true),
                    ValorDiaria = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alugueis", x => x.AluguelId);
                    table.ForeignKey(
                        name: "FK_Alugueis_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alugueis_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "VeiculoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CategoriasVeiculo",
                columns: new[] { "CategoriaVeiculoId", "Descricao", "Nome", "ValorDiariaBase" },
                values: new object[,]
                {
                    { 1, "Veículos econômicos", "Popular", 90.00m },
                    { 2, "Utilitários esportivos", "SUV", 180.00m },
                    { 3, "Veículos de alto padrão", "Luxo", 350.00m }
                });

            migrationBuilder.InsertData(
                table: "Fabricantes",
                columns: new[] { "FabricanteId", "Nome", "PaisOrigem" },
                values: new object[,]
                {
                    { 1, "Chevrolet", "Estados Unidos" },
                    { 2, "Fiat", "Itália" },
                    { 3, "Toyota", "Japão" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_ClienteId",
                table: "Alugueis",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alugueis_VeiculoId",
                table: "Alugueis",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasVeiculo_Nome",
                table: "CategoriasVeiculo",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CPF",
                table: "Clientes",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Email",
                table: "Clientes",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_Nome",
                table: "Fabricantes",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_CategoriaVeiculoId",
                table: "Veiculos",
                column: "CategoriaVeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_FabricanteId",
                table: "Veiculos",
                column: "FabricanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_Placa",
                table: "Veiculos",
                column: "Placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alugueis");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Veiculos");

            migrationBuilder.DropTable(
                name: "CategoriasVeiculo");

            migrationBuilder.DropTable(
                name: "Fabricantes");
        }
    }
}
