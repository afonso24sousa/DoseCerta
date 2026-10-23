using System;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoseCerta.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Retira a F007 (tarefas de cuidados recorrentes) do esquema, depois da alteração autorizada de âmbito: remove o gatilho
    /// e as quatro tabelas da F007 e recria as restrições das notificações sem 'TarefaNaoConfirmada' e 'ExecucaoTarefa'
    /// (modelo de dados v02, secção 15). A migração inicial não é alterada.
    /// </summary>
    public partial class RetirarTarefasRecorrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O gatilho sai antes das tabelas (modelo de dados v02, secção 15).
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS dbo.{GatilhosSql.NomeGatilhoExecucaoTarefa};");

            migrationBuilder.DropTable(
                name: "ExecucaoTarefa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HorarioTarefa",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TarefaDiaSemana",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TarefaRecorrente",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacao_Referencia",
                schema: "dbo",
                table: "Notificacao");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacao_Tipo",
                schema: "dbo",
                table: "Notificacao");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notificacao_Referencia",
                schema: "dbo",
                table: "Notificacao",
                sql: "([Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares') AND [ReferenciaTipo] = 'Toma') OR ([Tipo] = 'NovaOcorrencia' AND [ReferenciaTipo] = 'Ocorrencia') OR ([Tipo] = 'ProximoCompromisso' AND [ReferenciaTipo] = 'Compromisso') OR ([Tipo] = 'ConviteRecebido' AND [ReferenciaTipo] = 'Convite')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notificacao_Tipo",
                schema: "dbo",
                table: "Notificacao",
                sql: "[Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares','NovaOcorrencia','ProximoCompromisso','ConviteRecebido')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacao_Referencia",
                schema: "dbo",
                table: "Notificacao");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacao_Tipo",
                schema: "dbo",
                table: "Notificacao");

            migrationBuilder.CreateTable(
                name: "TarefaRecorrente",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Ativa = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1"),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CriadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FichaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntervaloDias = table.Column<byte>(type: "tinyint", nullable: true),
                    PeriodicidadeTipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToleranciaMinutos = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaRecorrente", x => x.Id);
                    table.CheckConstraint("CK_Tarefa_Intervalo", "([PeriodicidadeTipo] = 'CadaNDias' AND [IntervaloDias] BETWEEN 2 AND 30) OR ([PeriodicidadeTipo] <> 'CadaNDias' AND [IntervaloDias] IS NULL)");
                    table.CheckConstraint("CK_Tarefa_Periodicidade", "[PeriodicidadeTipo] IN ('Diaria','DiasSemana','CadaNDias')");
                    table.CheckConstraint("CK_Tarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
                    table.ForeignKey(
                        name: "FK_TarefaRecorrente_CriadaPorId",
                        column: x => x.CriadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TarefaRecorrente_FichaId",
                        column: x => x.FichaId,
                        principalSchema: "dbo",
                        principalTable: "Ficha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExecucaoTarefa",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ConfirmadaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ConfirmadaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CuidadoresNotificadosEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Designacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'PorConfirmar'"),
                    PrevistaEmUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToleranciaMinutos = table.Column<short>(type: "smallint", nullable: false),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucaoTarefa", x => x.Id);
                    table.CheckConstraint("CK_ExecucaoTarefa_Confirmacao", "([Estado] = 'Realizada' AND [ConfirmadaPorId] IS NOT NULL AND [ConfirmadaEmUtc] IS NOT NULL) OR ([Estado] = 'PorConfirmar' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL)");
                    table.CheckConstraint("CK_ExecucaoTarefa_Estado", "[Estado] IN ('PorConfirmar','Realizada')");
                    table.CheckConstraint("CK_ExecucaoTarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
                    table.ForeignKey(
                        name: "FK_ExecucaoTarefa_ConfirmadaPorId",
                        column: x => x.ConfirmadaPorId,
                        principalSchema: "dbo",
                        principalTable: "Utilizador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecucaoTarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HorarioTarefa",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    HoraLocal = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioTarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorarioTarefa_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TarefaDiaSemana",
                schema: "dbo",
                columns: table => new
                {
                    TarefaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarefaDiaSemana", x => new { x.TarefaId, x.DiaSemana });
                    table.CheckConstraint("CK_TarefaDiaSemana_Dia", "[DiaSemana] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_TarefaDiaSemana_TarefaId",
                        column: x => x.TarefaId,
                        principalSchema: "dbo",
                        principalTable: "TarefaRecorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notificacao_Referencia",
                schema: "dbo",
                table: "Notificacao",
                sql: "([Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares') AND [ReferenciaTipo] = 'Toma') OR ([Tipo] = 'TarefaNaoConfirmada' AND [ReferenciaTipo] = 'ExecucaoTarefa') OR ([Tipo] = 'NovaOcorrencia' AND [ReferenciaTipo] = 'Ocorrencia') OR ([Tipo] = 'ProximoCompromisso' AND [ReferenciaTipo] = 'Compromisso') OR ([Tipo] = 'ConviteRecebido' AND [ReferenciaTipo] = 'Convite')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notificacao_Tipo",
                schema: "dbo",
                table: "Notificacao",
                sql: "[Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares','TarefaNaoConfirmada','NovaOcorrencia','ProximoCompromisso','ConviteRecebido')");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoTarefa_PorConfirmar",
                schema: "dbo",
                table: "ExecucaoTarefa",
                column: "PrevistaEmUtc",
                filter: "[Estado] = 'PorConfirmar' AND [CuidadoresNotificadosEmUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_ExecucaoTarefa_TarefaInstante",
                schema: "dbo",
                table: "ExecucaoTarefa",
                columns: new[] { "TarefaId", "PrevistaEmUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_HorarioTarefa_TarefaHora",
                schema: "dbo",
                table: "HorarioTarefa",
                columns: new[] { "TarefaId", "HoraLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tarefa_FichaAtiva",
                schema: "dbo",
                table: "TarefaRecorrente",
                columns: new[] { "FichaId", "Ativa" });

            migrationBuilder.Sql(GatilhosSql.CriarGatilhoExecucaoTarefa);
        }
    }
}
