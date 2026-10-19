// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>TarefaDiaSemana</c> (dicionário de dados, secção 6.17).
/// </summary>
internal sealed class TarefaDiaSemanaConfiguracao : IEntityTypeConfiguration<TarefaDiaSemana>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TarefaDiaSemana> builder)
    {
        builder.ToTable("TarefaDiaSemana", t => t.HasCheckConstraint("CK_TarefaDiaSemana_Dia", "[DiaSemana] BETWEEN 1 AND 7"));
        builder.HasKey(e => new { e.TarefaId, e.DiaSemana }).HasName("PK_TarefaDiaSemana");
        builder.HasOne<TarefaRecorrente>()
            .WithMany()
            .HasForeignKey(e => e.TarefaId)
            .HasConstraintName("FK_TarefaDiaSemana_TarefaId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
