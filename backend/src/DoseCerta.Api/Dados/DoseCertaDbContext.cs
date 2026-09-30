// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DoseCerta.Api.Dados;

/// <summary>
/// Contexto de dados do DoseCerta (SQL Server 2022).
/// </summary>
public class DoseCertaDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoseCertaDbContext"/> class.
    /// </summary>
    /// <param name="options">Opções do contexto.</param>
    public DoseCertaDbContext(DbContextOptions<DoseCertaDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets os Utilizadores.
    /// </summary>
    public DbSet<Utilizador> Utilizadores => this.Set<Utilizador>();

    /// <summary>
    /// Gets as fichas.
    /// </summary>
    public DbSet<Ficha> Fichas => this.Set<Ficha>();

    /// <summary>
    /// Gets as associações de Utilizadores a fichas.
    /// </summary>
    public DbSet<AssociacaoFicha> AssociacoesFicha => this.Set<AssociacaoFicha>();

    /// <summary>
    /// Gets os documentos das fichas.
    /// </summary>
    public DbSet<DocumentoFicha> DocumentosFicha => this.Set<DocumentoFicha>();

    /// <summary>
    /// Gets a categoria fechada de condições de saúde.
    /// </summary>
    public DbSet<CondicaoSaude> CondicoesSaude => this.Set<CondicaoSaude>();

    /// <summary>
    /// Gets a categoria fechada de alergias.
    /// </summary>
    public DbSet<Alergia> Alergias => this.Set<Alergia>();

    /// <summary>
    /// Gets a categoria fechada de graus de autonomia.
    /// </summary>
    public DbSet<GrauAutonomia> GrausAutonomia => this.Set<GrauAutonomia>();

    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.GarantirDonoDaFichaPropriaInalterado();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.GarantirDonoDaFichaPropriaInalterado();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoseCertaDbContext).Assembly);
    }

    private static bool EraCuidadorPrincipal(EntityEntry<AssociacaoFicha> entrada) =>
        entrada.Property(a => a.EhCuidadorPrincipal).OriginalValue
        && entrada.Property(a => a.Papel).OriginalValue == PapelFicha.Cuidador;

    private static RegraNegocioException DonoImutavel() =>
        new(
            CodigoErro.CuidadorPrincipalFichaPropriaImutavel,
            "Na ficha própria, o dono é sempre o Cuidador principal e não pode ser despromovido nem removido.");

    /// <summary>
    /// Segunda linha de defesa da regra da F003: mesmo que outro código (por exemplo, da F004) altere
    /// diretamente uma associação sem passar por <see cref="Ficha"/>, o dono da ficha própria não pode
    /// ser despromovido, deixar de ser Cuidador principal, nem ser removido enquanto a ficha existir.
    /// </summary>
    private void GarantirDonoDaFichaPropriaInalterado()
    {
        foreach (var entrada in this.ChangeTracker.Entries<Ficha>().Where(e => e.State == EntityState.Modified))
        {
            var proprietario = entrada.Property(f => f.UtilizadorProprietarioId);
            if (proprietario.IsModified && proprietario.OriginalValue is not null)
            {
                throw DonoImutavel();
            }
        }

        var associacoesAlteradas = this.ChangeTracker.Entries<AssociacaoFicha>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entrada in associacoesAlteradas)
        {
            if (!EraCuidadorPrincipal(entrada) || this.FichaEstaASerEliminada(entrada.Entity.FichaId))
            {
                continue;
            }

            var mantemFuncao = entrada.State == EntityState.Modified
                && entrada.Entity.Papel == PapelFicha.Cuidador
                && entrada.Entity.EhCuidadorPrincipal;

            if (!mantemFuncao && this.EhDonoDaFichaPropria(entrada.Entity.FichaId, entrada.Entity.UtilizadorId))
            {
                throw DonoImutavel();
            }
        }
    }

    private bool FichaEstaASerEliminada(Guid fichaId) =>
        this.ChangeTracker.Entries<Ficha>().Any(e => e.Entity.Id == fichaId && e.State == EntityState.Deleted);

    private bool EhDonoDaFichaPropria(Guid fichaId, Guid utilizadorId)
    {
        var ficha = this.ChangeTracker.Entries<Ficha>().FirstOrDefault(e => e.Entity.Id == fichaId)?.Entity;
        if (ficha is not null)
        {
            return ficha.UtilizadorProprietarioId == utilizadorId;
        }

        return this.Fichas.AsNoTracking().Any(f => f.Id == fichaId && f.UtilizadorProprietarioId == utilizadorId);
    }
}
