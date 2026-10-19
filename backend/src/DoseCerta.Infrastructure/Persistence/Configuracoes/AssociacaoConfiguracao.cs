// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Associacao</c> (dicionário de dados, secção 6.9).
/// </summary>
internal sealed class AssociacaoConfiguracao : IEntityTypeConfiguration<Associacao>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Associacao> builder)
    {
        builder.ToTable("Associacao", t =>
        {
            t.HasCheckConstraint("CK_Associacao_Papel", "[Papel] IN ('Cuidador','Familiar')");
            t.HasCheckConstraint("CK_Associacao_Principal", "[EPrincipal] = 0 OR [Papel] = 'Cuidador'");
            t.HasCheckConstraint("CK_Associacao_Provisorio", "[EProvisorio] = 0 OR [EPrincipal] = 1");
        });
        builder.HasKey(e => e.Id).HasName("PK_Associacao");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Papel).ComoTexto(10);
        builder.Property(e => e.EPrincipal).LogicoPorOmissao(false);
        builder.Property(e => e.EProvisorio).LogicoPorOmissao(false);
        builder.Property(e => e.CriadaEmUtc).InstanteUtcPorOmissao();
        builder.HasOne(e => e.Utilizador)
            .WithMany()
            .HasForeignKey(e => e.UtilizadorId)
            .HasConstraintName("FK_Associacao_UtilizadorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.UtilizadorId }, "UQ_Associacao_FichaUtilizador").IsUnique();
        builder.HasIndex(e => e.FichaId, "UQ_Associacao_Principal").IsUnique().HasFilter("[EPrincipal] = 1");
        builder.HasIndex(e => new { e.UtilizadorId, e.FichaId }, "IX_Associacao_Utilizador").IncludeProperties(e => e.Papel);
    }
}
