// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da retirada da F007 (I043): depois de aplicadas todas as migrações não resta nenhuma tabela,
/// gatilho nem valor de restrição das tarefas recorrentes (modelo de dados v02, secção 15).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class RetiradaF007Testes
{
    private const int ErroRestricaoCheck = 547;

    private static readonly string[] TabelasDaF007 = { "TarefaRecorrente", "HorarioTarefa", "TarefaDiaSemana", "ExecucaoTarefa" };

    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetiradaF007Testes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public RetiradaF007Testes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// As quatro tabelas e o gatilho <c>TR_ExecucaoTarefa_Historico</c> da F007 não existem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TabelasEGatilhoDaF007NaoExistem()
    {
        var objetos = await this.fabrica.ComContextoAsync(c => c.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.objects WHERE type IN ('U', 'TR')")
            .ToListAsync());

        Assert.All(TabelasDaF007, t => Assert.DoesNotContain(t, objetos));
        Assert.DoesNotContain(GatilhosSql.NomeGatilhoExecucaoTarefa, objetos);
    }

    /// <summary>
    /// Ficam 19 tabelas de domínio (sem contar <c>__EFMigrationsHistory</c>) e 3 gatilhos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FicamDezanoveTabelasETresGatilhos()
    {
        var (tabelas, gatilhos) = await this.fabrica.ComContextoAsync(async c => (
            await c.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name <> '__EFMigrationsHistory'").SingleAsync(),
            await c.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE parent_class = 1").SingleAsync()));

        Assert.Equal(19, tabelas);
        Assert.Equal(3, gatilhos);
    }

    /// <summary>
    /// <c>CK_Notificacao_Tipo</c> recusa o tipo <c>TarefaNaoConfirmada</c> e <c>CK_Notificacao_Referencia</c> recusa a
    /// referência <c>ExecucaoTarefa</c>; uma notificação válida com os mesmos dados é aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RestricoesDasNotificacoesRecusamOsValoresDaF007()
    {
        var (fichaId, destinatarioId) = await this.fabrica.ComContextoAsync(async c =>
        {
            var utilizador = await DadosTeste.CriarUtilizadorAsync(c);
            var ficha = await DadosTeste.CriarFichaAsync(c, utilizador.Id);
            return (ficha.Id, utilizador.Id);
        });

        var tipo = await Assert.ThrowsAsync<SqlException>(() => this.InserirNotificacaoAsync(fichaId, destinatarioId, "TarefaNaoConfirmada", "Toma"));
        var referencia = await Assert.ThrowsAsync<SqlException>(() => this.InserirNotificacaoAsync(fichaId, destinatarioId, "TomaEmAtraso", "ExecucaoTarefa"));
        var valida = await this.InserirNotificacaoAsync(fichaId, destinatarioId, "TomaEmAtraso", "Toma");

        Assert.Equal(ErroRestricaoCheck, tipo.Number);
        Assert.Contains("CK_Notificacao_", tipo.Message, StringComparison.Ordinal);
        Assert.Equal(ErroRestricaoCheck, referencia.Number);
        Assert.Contains("CK_Notificacao_Referencia", referencia.Message, StringComparison.Ordinal);
        Assert.Equal(1, valida);
    }

    private Task<int> InserirNotificacaoAsync(Guid fichaId, Guid destinatarioId, string tipo, string referenciaTipo)
    {
        return this.fabrica.ComContextoAsync(c => c.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.Notificacao (Id, DestinatarioId, FichaId, Tipo, Mensagem, ReferenciaTipo, ReferenciaId, InstanteLimiteUtc) VALUES ({Guid.NewGuid()}, {destinatarioId}, {fichaId}, {tipo}, N'Notificação fictícia.', {referenciaTipo}, {Guid.NewGuid()}, SYSUTCDATETIME())"));
    }
}
