// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>HorarioToma</c> (dicionário de dados, secção 6.12).
/// </summary>
internal sealed class HorarioTomaConfiguracao : IEntityTypeConfiguration<HorarioToma>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<HorarioToma> builder)
    {
        builder.ToTable("HorarioToma");
        builder.HasKey(e => e.Id).HasName("PK_HorarioToma");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.HoraLocal).HoraLocal();
        builder.HasOne<Tratamento>()
            .WithMany()
            .HasForeignKey(e => e.TratamentoId)
            .HasConstraintName("FK_HorarioToma_TratamentoId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TratamentoId, e.HoraLocal }, "UQ_HorarioToma_TratamentoHora").IsUnique();
    }
}
