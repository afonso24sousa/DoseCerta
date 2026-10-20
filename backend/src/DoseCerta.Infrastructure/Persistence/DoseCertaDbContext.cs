// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Contexto do Entity Framework Core 8 (code-first) sobre SQL Server 2022 (AD-04, RNF-053, RNF-057).
/// O mapeamento de cada entidade está numa classe de configuração própria, na pasta Configuracoes, e segue
/// o script T-SQL da secção 13.2 do modelo de dados (nomes de chaves, restrições, índices e gatilhos).
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

    /// <summary>
    /// Gets as contas dos Utilizadores.
    /// </summary>
    public DbSet<Utilizador> Utilizadores => this.Set<Utilizador>();

    /// <summary>
    /// Gets as credenciais terminadas.
    /// </summary>
    public DbSet<SessaoRevogada> SessoesRevogadas => this.Set<SessaoRevogada>();

    /// <summary>
    /// Gets a lista fechada de condições de saúde.
    /// </summary>
    public DbSet<CondicaoSaude> CondicoesSaude => this.Set<CondicaoSaude>();

    /// <summary>
    /// Gets a lista fechada de alergias.
    /// </summary>
    public DbSet<Alergia> Alergias => this.Set<Alergia>();

    /// <summary>
    /// Gets a lista fechada de graus de autonomia.
    /// </summary>
    public DbSet<GrauAutonomia> GrausAutonomia => this.Set<GrauAutonomia>();

    /// <summary>
    /// Gets as fichas.
    /// </summary>
    public DbSet<Ficha> Fichas => this.Set<Ficha>();

    /// <summary>
    /// Gets as associações de pessoas às fichas.
    /// </summary>
    public DbSet<Associacao> Associacoes => this.Set<Associacao>();

    /// <summary>
    /// Gets os convites.
    /// </summary>
    public DbSet<Convite> Convites => this.Set<Convite>();

    /// <summary>
    /// Gets os documentos.
    /// </summary>
    public DbSet<Documento> Documentos => this.Set<Documento>();

    /// <summary>
    /// Gets os tratamentos.
    /// </summary>
    public DbSet<Tratamento> Tratamentos => this.Set<Tratamento>();

    /// <summary>
    /// Gets os horários das tomas.
    /// </summary>
    public DbSet<HorarioToma> HorariosToma => this.Set<HorarioToma>();

    /// <summary>
    /// Gets as tomas.
    /// </summary>
    public DbSet<Toma> Tomas => this.Set<Toma>();

    /// <summary>
    /// Gets os compromissos.
    /// </summary>
    public DbSet<Compromisso> Compromissos => this.Set<Compromisso>();

    /// <summary>
    /// Gets as ocorrências.
    /// </summary>
    public DbSet<Ocorrencia> Ocorrencias => this.Set<Ocorrencia>();

    /// <summary>
    /// Gets as notas.
    /// </summary>
    public DbSet<Nota> Notas => this.Set<Nota>();

    /// <summary>
    /// Gets as notificações.
    /// </summary>
    public DbSet<Notificacao> Notificacoes => this.Set<Notificacao>();

    /// <summary>
    /// Gets os envios de e-mail.
    /// </summary>
    public DbSet<EnvioEmail> EnviosEmail => this.Set<EnvioEmail>();

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
