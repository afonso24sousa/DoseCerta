// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Convite</c> (dicionário de dados, secção 6.10).
/// </summary>
internal sealed class ConviteConfiguracao : IEntityTypeConfiguration<Convite>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Convite> builder)
    {
        builder.ToTable("Convite", t =>
        {
            t.HasCheckConstraint("CK_Convite_Papel", "[PapelOferecido] IN ('Cuidador','Familiar')");
            t.HasCheckConstraint("CK_Convite_Principal", "[ParaPrincipal] = 0 OR [PapelOferecido] = 'Cuidador'");
            t.HasCheckConstraint("CK_Convite_Estado", "[Estado] IN ('Pendente','Aceite','Recusado')");
            t.HasCheckConstraint(
                "CK_Convite_Resposta",
                "([Estado] = 'Pendente' AND [RespondidoEmUtc] IS NULL AND [UtilizadorConvidadoId] IS NULL) OR ([Estado] <> 'Pendente' AND [RespondidoEmUtc] IS NOT NULL AND [UtilizadorConvidadoId] IS NOT NULL)");
        });
        builder.HasKey(e => e.Id).HasName("PK_Convite");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.EmailConvidado).HasMaxLength(254).UseCollation(ExtensoesConfiguracao.CollationEmail);
        builder.Property(e => e.PapelOferecido).ComoTexto(10);
        builder.Property(e => e.ParaPrincipal).LogicoPorOmissao(false);
        builder.Property(e => e.Estado).ComoTexto(10).EstadoPorOmissao(EstadoConvite.Pendente);
        builder.Property(e => e.CriadoEmUtc).InstanteUtcPorOmissao();
        builder.Property(e => e.RespondidoEmUtc).InstanteUtc();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Convite_FichaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.ConvidadoPorId)
            .HasConstraintName("FK_Convite_ConvidadoPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.UtilizadorConvidadoId)
            .HasConstraintName("FK_Convite_UtilizadorConvidadoId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.EmailConvidado }, "UQ_Convite_Pendente")
            .IsUnique()
            .HasFilter("[Estado] = 'Pendente'");
        builder.HasIndex(e => new { e.EmailConvidado, e.Estado }, "IX_Convite_EmailEstado");
    }
}
