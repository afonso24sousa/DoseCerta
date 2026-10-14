// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>TarefaRecorrente</c> (dicionário de dados, secção 6.15).
/// </summary>
internal sealed class TarefaRecorrenteConfiguracao : IEntityTypeConfiguration<TarefaRecorrente>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TarefaRecorrente> builder)
    {
        builder.ToTable("TarefaRecorrente", t =>
        {
            t.HasCheckConstraint("CK_Tarefa_Periodicidade", "[PeriodicidadeTipo] IN ('Diaria','DiasSemana','CadaNDias')");
            t.HasCheckConstraint(
                "CK_Tarefa_Intervalo",
                "([PeriodicidadeTipo] = 'CadaNDias' AND [IntervaloDias] BETWEEN 2 AND 30) OR ([PeriodicidadeTipo] <> 'CadaNDias' AND [IntervaloDias] IS NULL)");
            t.HasCheckConstraint("CK_Tarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
        });
        builder.HasKey(e => e.Id).HasName("PK_TarefaRecorrente");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Designacao).HasMaxLength(200);
        builder.Property(e => e.PeriodicidadeTipo).ComoTexto(10);
        builder.Property(e => e.DataInicio).HasColumnType("date");
        builder.Property(e => e.Ativa).LogicoPorOmissao(true);
        builder.Property(e => e.CriadaEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_TarefaRecorrente_FichaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.CriadaPorId)
            .HasConstraintName("FK_TarefaRecorrente_CriadaPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.Ativa }, "IX_Tarefa_FichaAtiva");
    }
}
