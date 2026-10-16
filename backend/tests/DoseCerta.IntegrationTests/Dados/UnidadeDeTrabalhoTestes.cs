// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da unidade de trabalho sobre o SQL Server (RNF-046, AD-10). Como ainda não existem
/// entidades, a escrita de teste é a criação de uma tabela de nome único (o DDL é transacional no SQL Server).
/// </summary>
[Collection(ColecaoApi.Nome)]
public class UnidadeDeTrabalhoTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public UnidadeDeTrabalhoTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// Uma operação bem-sucedida fica gravada e as compensações não são executadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OperacaoBemSucedidaFicaGravada()
    {
        var tabela = NomeTabelaUnico();
        var compensada = false;

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            await unidade.ExecutarEmTransacaoAsync(
                async () =>
                {
                    unidade.AoFalhar(() => compensada = true);
                    await CriarTabelaAsync(contexto, tabela);
                    await unidade.GravarAlteracoesAsync(CancellationToken.None);
                    return true;
                },
                CancellationToken.None);
        }

        Assert.True(await this.TabelaExisteAsync(tabela));
        Assert.False(compensada);
    }

    /// <summary>
    /// Uma operação que falha não grava nada, executa as compensações registadas (mesmo que uma delas falhe)
    /// e volta a lançar o erro original.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OperacaoFalhadaNaoGravaNadaEExecutaCompensacoes()
    {
        var tabela = NomeTabelaUnico();
        var compensada = false;

        await using (var ambito = this.fabrica.Services.CreateAsyncScope())
        {
            var unidade = ambito.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => unidade.ExecutarEmTransacaoAsync<bool>(
                async () =>
                {
                    unidade.AoFalhar(() => throw new IOException("Falha simulada da compensação."));
                    unidade.AoFalhar(() => compensada = true);
                    await CriarTabelaAsync(contexto, tabela);
                    throw new InvalidOperationException("Falha simulada.");
                },
                CancellationToken.None));
        }

        Assert.False(await this.TabelaExisteAsync(tabela));
        Assert.True(compensada);
    }

    private static string NomeTabelaUnico()
    {
        return $"TesteTransacao_{Guid.NewGuid():N}";
    }

    private static Task<int> CriarTabelaAsync(DoseCertaDbContext contexto, string tabela)
    {
        // O nome é gerado pelo próprio teste (letras, algarismos e sublinhado), por isso pode ser composto no comando.
#pragma warning disable EF1002
        return contexto.Database.ExecuteSqlRawAsync($"CREATE TABLE dbo.[{tabela}] (Id int NOT NULL);");
#pragma warning restore EF1002
    }

    private async Task<bool> TabelaExisteAsync(string tabela)
    {
        var total = await this.fabrica.ComContextoAsync(c =>
            c.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = {tabela}").SingleAsync());
        return total == 1;
    }
}
