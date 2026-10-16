// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>ExecucaoTarefa</c> (dicionário de dados, secção 6.18).
/// </summary>
internal sealed class ExecucaoTarefaConfiguracao : IEntityTypeConfiguration<ExecucaoTarefa>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ExecucaoTarefa> builder)
    {
        builder.ToTable("ExecucaoTarefa", t =>
        {
            t.HasCheckConstraint("CK_ExecucaoTarefa_Estado", "[Estado] IN ('PorConfirmar','Realizada')");
            t.HasCheckConstraint(
                "CK_ExecucaoTarefa_Confirmacao",
                "([Estado] = 'Realizada' AND [ConfirmadaPorId] IS NOT NULL AND [ConfirmadaEmUtc] IS NOT NULL) OR ([Estado] = 'PorConfirmar' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL)");
            t.HasCheckConstraint("CK_ExecucaoTarefa_Tolerancia", "[ToleranciaMinutos] BETWEEN 15 AND 1440");
            t.HasTrigger(GatilhosSql.NomeGatilhoExecucaoTarefa);
        });
        builder.HasKey(e => e.Id).HasName("PK_ExecucaoTarefa");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.PrevistaEmUtc).InstanteUtc();
        builder.Property(e => e.Designacao).HasMaxLength(200);
        builder.Property(e => e.Estado).ComoTexto(12).EstadoPorOmissao(EstadoExecucaoTarefa.PorConfirmar);
        builder.Property(e => e.ConfirmadaEmUtc).InstanteUtc();
        builder.Property(e => e.CuidadoresNotificadosEmUtc).InstanteUtc();
        builder.Property(e => e.Versao).IsRowVersion();
        builder.HasOne<TarefaRecorrente>()
            .WithMany()
            .HasForeignKey(e => e.TarefaId)
            .HasConstraintName("FK_ExecucaoTarefa_TarefaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.ConfirmadaPorId)
            .HasConstraintName("FK_ExecucaoTarefa_ConfirmadaPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.TarefaId, e.PrevistaEmUtc }, "UQ_ExecucaoTarefa_TarefaInstante").IsUnique();
        builder.HasIndex(e => e.PrevistaEmUtc, "IX_ExecucaoTarefa_PorConfirmar")
            .HasFilter("[Estado] = 'PorConfirmar' AND [CuidadoresNotificadosEmUtc] IS NULL");
    }
}
