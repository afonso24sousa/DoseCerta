// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Contexto do Entity Framework Core 8 (code-first) sobre SQL Server 2022 (AD-04, RNF-053, RNF-057).
/// Nesta versão não tem entidades: serve para validar a ligação ao SQL Server e a configuração da persistência.
/// O mapeamento de cada entidade ficará numa classe <c>IEntityTypeConfiguration</c> própria deste assembly,
/// aplicada automaticamente em <see cref="OnModelCreating"/>.
/// </summary>
public class DoseCertaDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoseCertaDbContext"/> class.
    /// </summary>
    /// <param name="opcoes">Opções do contexto.</param>
    public DoseCertaDbContext(DbContextOptions<DoseCertaDbContext> opcoes)
        : base(opcoes)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // O script do modelo de dados (secção 13.2) define explicitamente todos os índices;
        // o EF Core não cria índices adicionais para as chaves estrangeiras.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
        base.ConfigureConventions(configurationBuilder);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoseCertaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
