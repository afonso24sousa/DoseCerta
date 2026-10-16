// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários do mapeamento EF Core do modelo de dados, sem base de dados (I030; modelo de dados, secção 13.2).
/// </summary>
public class ModeloEfTestes
{
    /// <summary>
    /// O modelo mapeia as 23 tabelas do esquema dbo, incluindo as duas tabelas de junção das necessidades.
    /// </summary>
    [Fact]
    public void ModeloMapeiaAsVinteETresTabelas()
    {
        var modelo = Modelo();

        var tabelas = modelo.GetEntityTypes().Select(e => e.GetTableName()).Distinct().ToList();

        Assert.Equal(23, tabelas.Count);
        Assert.Contains("FichaCondicaoSaude", tabelas);
        Assert.Contains("FichaAlergia", tabelas);
        Assert.All(modelo.GetEntityTypes(), e => Assert.Equal("dbo", e.GetSchema()));
    }

    /// <summary>
    /// As tabelas com gatilhos declaram-nos, para o EF Core não usar OUTPUT nas gravações dessas tabelas.
    /// </summary>
    [Fact]
    public void TabelasComGatilhosDeclaramOsGatilhos()
    {
        var declarados = Modelo().GetEntityTypes()
            .SelectMany(e => e.GetDeclaredTriggers())
            .Select(t => t.GetDatabaseName())
            .ToList();

        Assert.Equal(GatilhosSql.Nomes().OrderBy(n => n, StringComparer.Ordinal), declarados.OrderBy(n => n, StringComparer.Ordinal));
    }

    /// <summary>
    /// As listas fechadas têm os valores predefinidos da secção 8 do modelo de dados (9 condições, 6 alergias, 4 graus).
    /// </summary>
    [Fact]
    public void ListasFechadasTemValoresPredefinidos()
    {
        var modelo = Modelo();

        Assert.Equal(9, Dados<CondicaoSaude>(modelo).Count);
        Assert.Equal(6, Dados<Alergia>(modelo).Count);
        Assert.Equal(4, Dados<GrauAutonomia>(modelo).Count);
    }

    /// <summary>
    /// O e-mail usa a collation sem distinção de maiúsculas nem de acentos e é único (UQ_Utilizador_Email, RF-011).
    /// </summary>
    [Fact]
    public void EmailEUnicoSemDistinguirMaiusculas()
    {
        var utilizador = Modelo().FindEntityType(typeof(Utilizador))!;

        Assert.Equal("Latin1_General_100_CI_AI", utilizador.FindProperty(nameof(Utilizador.Email))!.GetCollation());
        var indice = Assert.Single(utilizador.GetIndexes(), i => i.GetDatabaseName() == "UQ_Utilizador_Email");
        Assert.True(indice.IsUnique);
    }

    /// <summary>
    /// Eliminar uma ficha elimina as associações por cascata; um Utilizador referenciado não é eliminado (NO ACTION).
    /// </summary>
    [Fact]
    public void RegrasDeEliminacaoSeguemOModelo()
    {
        var chaves = Modelo().GetEntityTypes().SelectMany(e => e.GetForeignKeys()).ToList();

        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(chaves, c => c.GetConstraintName() == "FK_Associacao_FichaId").DeleteBehavior);
        Assert.Equal(DeleteBehavior.NoAction, Assert.Single(chaves, c => c.GetConstraintName() == "FK_Ficha_CriadaPorId").DeleteBehavior);
    }

    /// <summary>
    /// A migração inicial cria os quatro gatilhos no fim do Up e remove-os no início do Down.
    /// </summary>
    [Fact]
    public void GatilhosSaoCriadosERemovidosPelaMigracao()
    {
        var criacao = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var remocao = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        criacao.CriarGatilhosDoseCerta();
        remocao.RemoverGatilhosDoseCerta();

        var criados = criacao.Operations.Cast<SqlOperation>().Select(o => o.Sql).ToList();
        var removidos = remocao.Operations.Cast<SqlOperation>().Select(o => o.Sql).ToList();
        Assert.Equal(4, criados.Count);
        Assert.Equal(4, removidos.Count);
        foreach (var nome in GatilhosSql.Nomes())
        {
            Assert.Single(criados, sql => sql.Contains($"CREATE TRIGGER dbo.{nome} ", StringComparison.Ordinal));
            Assert.Contains($"DROP TRIGGER IF EXISTS dbo.{nome};", removidos);
        }
    }

    private static IModel Modelo()
    {
        // O modelo de design mantém os metadados das migrações (dados predefinidos, collations) que o modelo de execução omite.
        using var contexto = new FabricaDesignDoseCertaDbContext().CreateDbContext(Array.Empty<string>());
        return contexto.GetService<IDesignTimeModel>().Model;
    }

    private static List<IDictionary<string, object?>> Dados<T>(IModel modelo)
    {
        return modelo.FindEntityType(typeof(T))!.GetSeedData().ToList();
    }
}
