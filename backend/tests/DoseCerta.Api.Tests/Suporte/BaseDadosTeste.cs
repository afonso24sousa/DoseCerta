// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dados;
using DoseCerta.Api.Dominio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Api.Tests.Suporte;

/// <summary>
/// Base de dados SQLite em memória com o modelo do DoseCerta e Utilizadores fictícios.
/// </summary>
/// <remarks>
/// O SQLite respeita os índices únicos, as restrições CHECK e as transações do modelo; os testes contra
/// SQL Server 2022 correm na pipeline (QG-03).
/// </remarks>
public sealed class BaseDadosTeste : IDisposable
{
    /// <summary>
    /// Utilizador fictício que cria a ficha própria nos testes.
    /// </summary>
    public static readonly Guid Ana = new("aaaaaaaa-0000-0000-0000-000000000001");

    /// <summary>
    /// Segundo Utilizador fictício, convidado para as fichas.
    /// </summary>
    public static readonly Guid Bruno = new("bbbbbbbb-0000-0000-0000-000000000002");

    /// <summary>
    /// Terceiro Utilizador fictício, sem associações.
    /// </summary>
    public static readonly Guid Carla = new("cccccccc-0000-0000-0000-000000000003");

    private readonly SqliteConnection ligacao;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDadosTeste"/> class.
    /// </summary>
    public BaseDadosTeste()
    {
        this.ligacao = new SqliteConnection("DataSource=:memory:");
        this.ligacao.Open();

        using var contexto = this.CriarContexto();
        contexto.Database.EnsureCreated();
        contexto.Utilizadores.AddRange(
            new Utilizador { Id = Ana, Nome = "Ana Teste", Email = "ana@teste.local" },
            new Utilizador { Id = Bruno, Nome = "Bruno Teste", Email = "bruno@teste.local" },
            new Utilizador { Id = Carla, Nome = "Carla Teste", Email = "carla@teste.local" });
        contexto.SaveChanges();
    }

    /// <summary>
    /// Gets a ligação partilhada, para registar o contexto na API em testes de integração.
    /// </summary>
    public SqliteConnection Ligacao => this.ligacao;

    /// <summary>
    /// Cria um novo contexto sobre a mesma base de dados.
    /// </summary>
    /// <returns>O contexto.</returns>
    public DoseCertaDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<DoseCertaDbContext>().UseSqlite(this.ligacao).Options);

    /// <inheritdoc/>
    public void Dispose() => this.ligacao.Dispose();
}
