// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.RegularExpressions;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DoseCerta.IntegrationTests.Dados;

/// <summary>
/// Testes de integração da migração <c>RetirarTarefasRecorrentes</c> sobre dados existentes (I043): numa base de dados só com
/// a migração inicial e os dados fictícios do script <c>02</c>, a retirada da F007 não apaga contas, fichas, necessidades,
/// associações, convites nem notificações. Usa uma base de dados própria, porque a da coleção já tem todas as migrações.
/// </summary>
public sealed partial class RetiradaF007DadosTestes : IAsyncLifetime
{
    private const string MigracaoInicial = "20261014232229_Inicial";

    private static readonly string[] TabelasComDados =
    {
        "Utilizador", "Ficha", "FichaCondicaoSaude", "FichaAlergia", "Associacao", "Convite", "Notificacao",
    };

    private readonly DbContextOptions<DoseCertaDbContext> opcoes;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetiradaF007DadosTestes"/> class.
    /// </summary>
    public RetiradaF007DadosTestes()
    {
        var baseLigacao = Environment.GetEnvironmentVariable(FabricaApi.VariavelSqlServer)
            ?? throw new InvalidOperationException($"Defina a variável de ambiente {FabricaApi.VariavelSqlServer}.");
        var ligacao = new SqlConnectionStringBuilder(baseLigacao) { InitialCatalog = $"DoseCerta_Testes_{Guid.NewGuid():N}" };
        this.opcoes = new DbContextOptionsBuilder<DoseCertaDbContext>().UseSqlServer(ligacao.ConnectionString).Options;
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await using var contexto = new DoseCertaDbContext(this.opcoes);
        await contexto.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// As linhas de cada tabela com dados são as mesmas antes e depois da migração <c>RetirarTarefasRecorrentes</c>, incluindo
    /// uma notificação <c>ConviteRecebido</c>, que tem de continuar válida nas restrições recriadas pela migração.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RetirarTarefasRecorrentesNaoApagaOsDadosFicticios()
    {
        await using var contexto = new DoseCertaDbContext(this.opcoes);
        var migrador = contexto.GetService<IMigrator>();
        await migrador.MigrateAsync(MigracaoInicial);
        await contexto.Database.ExecuteSqlRawAsync(Separador().Replace(await File.ReadAllTextAsync(CaminhoScript02()), string.Empty));
        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO dbo.Notificacao (Id, DestinatarioId, FichaId, Tipo, Mensagem, ReferenciaTipo, ReferenciaId, InstanteLimiteUtc) "
            + "SELECT NEWID(), u.Id, c.FichaId, 'ConviteRecebido', N'Convite fictício.', 'Convite', c.Id, c.CriadoEmUtc "
            + "FROM dbo.Convite c JOIN dbo.Utilizador u ON u.Email = c.EmailConvidado");
        var antes = await LinhasAsync(contexto);

        await migrador.MigrateAsync();
        var depois = await LinhasAsync(contexto);

        Assert.All(TabelasComDados, t => Assert.NotEmpty(antes[t]));
        Assert.All(TabelasComDados, t => Assert.Equal(antes[t], depois[t]));
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }

    private static async Task<Dictionary<string, List<string>>> LinhasAsync(DoseCertaDbContext contexto)
    {
        var linhas = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var tabela in TabelasComDados)
        {
            // Cada linha inteira em JSON, ordenada, para comparar o conteúdo e não só a contagem.
            linhas[tabela] = await contexto.Database
                .SqlQueryRaw<string>("SELECT (SELECT t.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS [Value] FROM dbo." + tabela + " t")
                .ToListAsync();
            linhas[tabela].Sort(StringComparer.Ordinal);
        }

        return linhas;
    }

    private static string CaminhoScript02()
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            var caminho = Path.Combine(pasta.FullName, "scripts-base-dados", "02-dados-iniciais-ficticios.sql");
            if (File.Exists(caminho))
            {
                return caminho;
            }
        }

        throw new FileNotFoundException("Não foi encontrado o script scripts-base-dados/02-dados-iniciais-ficticios.sql.");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex Separador();
}
