// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Compromisso</c> (dicionário de dados, secção 6.19).
/// </summary>
internal sealed class CompromissoConfiguracao : IEntityTypeConfiguration<Compromisso>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Compromisso> builder)
    {
        builder.ToTable("Compromisso", t => t.HasCheckConstraint(
            "CK_Compromisso_Antecedencia",
            "[AntecedenciaAvisoMin] IN (30, 60, 120, 1440, 2880)"));
        builder.HasKey(e => e.Id).HasName("PK_Compromisso");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.DataHoraUtc).InstanteUtc();
        builder.Property(e => e.Local).HasMaxLength(150);
        builder.Property(e => e.Motivo).HasMaxLength(250);
        builder.Property(e => e.AntecedenciaAvisoMin).HasDefaultValue(1440).ValueGeneratedNever();
        builder.Property(e => e.AvisoEnviadoEmUtc).InstanteUtc();
        builder.Property(e => e.CriadoEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Compromisso_FichaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.CriadoPorId)
            .HasConstraintName("FK_Compromisso_CriadoPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.DataHoraUtc }, "IX_Compromisso_FichaData");
        builder.HasIndex(e => e.DataHoraUtc, "IX_Compromisso_AvisoPendente").HasFilter("[AvisoEnviadoEmUtc] IS NULL");
    }
}
