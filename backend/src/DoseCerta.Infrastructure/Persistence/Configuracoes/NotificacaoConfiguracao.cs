// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Notificacao</c> (dicionário de dados, secção 6.22).
/// </summary>
internal sealed class NotificacaoConfiguracao : IEntityTypeConfiguration<Notificacao>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacao", t =>
        {
            t.HasCheckConstraint(
                "CK_Notificacao_Tipo",
                "[Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares','TarefaNaoConfirmada','NovaOcorrencia','ProximoCompromisso','ConviteRecebido')");
            t.HasCheckConstraint(
                "CK_Notificacao_Referencia",
                "([Tipo] IN ('TomaEmAtraso','TomaGraveFamiliares') AND [ReferenciaTipo] = 'Toma') OR ([Tipo] = 'TarefaNaoConfirmada' AND [ReferenciaTipo] = 'ExecucaoTarefa') OR ([Tipo] = 'NovaOcorrencia' AND [ReferenciaTipo] = 'Ocorrencia') OR ([Tipo] = 'ProximoCompromisso' AND [ReferenciaTipo] = 'Compromisso') OR ([Tipo] = 'ConviteRecebido' AND [ReferenciaTipo] = 'Convite')");
        });
        builder.HasKey(e => e.Id).HasName("PK_Notificacao");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Tipo).ComoTexto(25);
        builder.Property(e => e.Mensagem).HasMaxLength(400);
        builder.Property(e => e.ReferenciaTipo).ComoTexto(20);
        builder.Property(e => e.InstanteLimiteUtc).InstanteUtc();
        builder.Property(e => e.CriadaEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.DestinatarioId)
            .HasConstraintName("FK_Notificacao_DestinatarioId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Notificacao_FichaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.DestinatarioId, e.Tipo, e.ReferenciaId, e.InstanteLimiteUtc }, "UQ_Notificacao_Origem").IsUnique();
        builder.HasIndex(e => new { e.DestinatarioId, e.CriadaEmUtc }, "IX_Notificacao_DestinatarioData").IsDescending(false, true);
    }
}
