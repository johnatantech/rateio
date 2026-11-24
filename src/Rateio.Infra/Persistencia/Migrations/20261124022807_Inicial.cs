using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rateio.Infra.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Participantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TipoChavePix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ChavePix = table.Column<string>(type: "nvarchar(77)", maxLength: 77, nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participantes_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Despesas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PagoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorCentavos = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesas", x => x.Id);
                    table.CheckConstraint("CK_Despesas_Valor", "[ValorCentavos] > 0");
                    table.ForeignKey(
                        name: "FK_Despesas_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Despesas_Participantes_PagoPorId",
                        column: x => x.PagoPorId,
                        principalTable: "Participantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorCentavos = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.CheckConstraint("CK_Pagamentos_Pessoas", "[DeId] <> [ParaId]");
                    table.CheckConstraint("CK_Pagamentos_Valor", "[ValorCentavos] > 0");
                    table.ForeignKey(
                        name: "FK_Pagamentos_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Participantes_DeId",
                        column: x => x.DeId,
                        principalTable: "Participantes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pagamentos_Participantes_ParaId",
                        column: x => x.ParaId,
                        principalTable: "Participantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DivisoesDespesa",
                columns: table => new
                {
                    DespesaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorCentavos = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisoesDespesa", x => new { x.DespesaId, x.ParticipanteId });
                    table.CheckConstraint("CK_DivisoesDespesa_Valor", "[ValorCentavos] >= 0");
                    table.ForeignKey(
                        name: "FK_DivisoesDespesa_Despesas_DespesaId",
                        column: x => x.DespesaId,
                        principalTable: "Despesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DivisoesDespesa_Participantes_ParticipanteId",
                        column: x => x.ParticipanteId,
                        principalTable: "Participantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_GrupoId_Data",
                table: "Despesas",
                columns: new[] { "GrupoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_PagoPorId",
                table: "Despesas",
                column: "PagoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisoesDespesa_ParticipanteId",
                table: "DivisoesDespesa",
                column: "ParticipanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_DeId",
                table: "Pagamentos",
                column: "DeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_GrupoId",
                table: "Pagamentos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ParaId",
                table: "Pagamentos",
                column: "ParaId");

            migrationBuilder.CreateIndex(
                name: "IX_Participantes_GrupoId_Nome",
                table: "Participantes",
                columns: new[] { "GrupoId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DivisoesDespesa");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "Despesas");

            migrationBuilder.DropTable(
                name: "Participantes");

            migrationBuilder.DropTable(
                name: "Grupos");
        }
    }
}
