// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Tratamento</c> (dicionário de dados, secção 6.11).
/// </summary>
internal sealed class TratamentoConfiguracao : IEntityTypeConfiguration<Tratamento>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Tratamento> builder)
    {
        builder.ToTable("Tratamento", t =>
        {
            t.HasCheckConstraint("CK_Tratamento_Grau", "[GrauImportancia] IN ('Leve','Moderado','Grave')");
            t.HasCheckConstraint("CK_Tratamento_Datas", "[DataFim] IS NULL OR [DataFim] >= [DataInicio]");
            t.HasCheckConstraint("CK_Tratamento_Estado", "[Estado] IN ('EmCurso','Terminado')");
            t.HasCheckConstraint(
                "CK_Tratamento_Termino",
                "([Estado] = 'Terminado' AND [TerminadoEmUtc] IS NOT NULL) OR ([Estado] = 'EmCurso' AND [TerminadoEmUtc] IS NULL)");
        });
        builder.HasKey(e => e.Id).HasName("PK_Tratamento");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Medicamento).HasMaxLength(120);
        builder.Property(e => e.Dose).HasMaxLength(60);
        builder.Property(e => e.GrauImportancia).ComoTexto(10);
        builder.Property(e => e.DataInicio).HasColumnType("date");
        builder.Property(e => e.DataFim).HasColumnType("date");
        builder.Property(e => e.Estado).ComoTexto(10).EstadoPorOmissao(EstadoTratamento.EmCurso);
        builder.Property(e => e.TerminadoEmUtc).InstanteUtc();
        builder.Property(e => e.CriadoEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Tratamento_FichaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.CriadoPorId)
            .HasConstraintName("FK_Tratamento_CriadoPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Horarios)
            .WithOne()
            .HasForeignKey(h => h.TratamentoId)
            .HasConstraintName("FK_HorarioToma_TratamentoId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.FichaId, e.Estado }, "IX_Tratamento_FichaEstado");
    }
}
