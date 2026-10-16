// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Arranque;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Arranca a API completa em memória contra uma base de dados SQL Server exclusiva desta execução,
/// criada a partir das migrações e eliminada no fim. A instância do SQL Server lê-se da variável
/// de ambiente <c>DOSECERTA_TESTES_SQLSERVER</c> (connection string sem o nome da base de dados).
/// </summary>
public class FabricaApi : WebApplicationFactory<PontoEntradaApi>, IAsyncLifetime
{
    /// <summary>
    /// Chave JWT usada apenas nos testes (não é um segredo de nenhum ambiente real).
    /// </summary>
    public const string ChaveJwtTestes = "chave-de-testes-dosecerta-g07-com-32-ou-mais-carateres";

    /// <summary>
    /// Variável de ambiente com a connection string da instância de testes.
    /// </summary>
    public const string VariavelSqlServer = "DOSECERTA_TESTES_SQLSERVER";

    /// <summary>
    /// Initializes a new instance of the <see cref="FabricaApi"/> class.
    /// </summary>
    public FabricaApi()
    {
        var baseLigacao = Environment.GetEnvironmentVariable(VariavelSqlServer);
        if (string.IsNullOrWhiteSpace(baseLigacao))
        {
            throw new InvalidOperationException(
                $"Defina a variável de ambiente {VariavelSqlServer} com a connection string da instância SQL Server de testes "
                + "(sem nome de base de dados), por exemplo: Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=true.");
        }

        var construtor = new SqlConnectionStringBuilder(baseLigacao)
        {
            InitialCatalog = $"DoseCerta_Testes_{Guid.NewGuid():N}",
        };
        this.LigacaoBaseDados = construtor.ConnectionString;
    }

    /// <summary>
    /// Gets a connection string da base de dados exclusiva desta execução.
    /// </summary>
    public string LigacaoBaseDados { get; }

    /// <summary>
    /// Cria a base de dados aplicando as migrações.
    /// </summary>
    /// <returns>Uma tarefa que termina quando a base de dados estiver pronta.</returns>
    public async Task InitializeAsync()
    {
        await using var ambito = this.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        await contexto.Database.MigrateAsync();
    }

    /// <summary>
    /// Elimina a base de dados desta execução.
    /// </summary>
    /// <returns>Uma tarefa que termina quando a base de dados estiver eliminada.</returns>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var ambito = this.Services.CreateAsyncScope())
        {
            var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
            await contexto.Database.EnsureDeletedAsync();
        }

        await this.DisposeAsync();
    }

    /// <summary>
    /// Executa uma ação com um contexto da base de dados de testes.
    /// </summary>
    /// <typeparam name="T">Tipo do resultado.</typeparam>
    /// <param name="acao">Ação a executar.</param>
    /// <returns>O resultado da ação.</returns>
    public async Task<T> ComContextoAsync<T>(Func<DoseCertaDbContext, Task<T>> acao)
    {
        ArgumentNullException.ThrowIfNull(acao);
        await using var ambito = this.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        return await acao(contexto);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testes");
        builder.ConfigureAppConfiguration((_, configuracao) => configuracao.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DoseCerta"] = this.LigacaoBaseDados,
            ["BaseDados:AplicarMigracoesNoArranque"] = "false",
            ["Jwt:Chave"] = ChaveJwtTestes,
        }));
    }
}
