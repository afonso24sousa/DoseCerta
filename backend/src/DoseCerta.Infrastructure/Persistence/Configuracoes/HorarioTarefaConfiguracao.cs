// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>HorarioTarefa</c> (dicionário de dados, secção 6.16).
/// </summary>
internal sealed class HorarioTarefaConfiguracao : IEntityTypeConfiguration<HorarioTarefa>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<HorarioTarefa> builder)
    {
        builder.ToTable("HorarioTarefa");
        builder.HasKey(e => e.Id).HasName("PK_HorarioTarefa");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.HoraLocal).HoraLocal();
        builder.HasOne<TarefaRecorrente>()
            .WithMany()
            .HasForeignKey(e => e.TarefaId)
            .HasConstraintName("FK_HorarioTarefa_TarefaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TarefaId, e.HoraLocal }, "UQ_HorarioTarefa_TarefaHora").IsUnique();
    }
}
