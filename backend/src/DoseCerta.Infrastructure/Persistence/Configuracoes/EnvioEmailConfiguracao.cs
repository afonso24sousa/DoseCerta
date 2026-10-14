// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>EnvioEmail</c> (dicionário de dados, secção 6.23).
/// </summary>
internal sealed class EnvioEmailConfiguracao : IEntityTypeConfiguration<EnvioEmail>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<EnvioEmail> builder)
    {
        builder.ToTable("EnvioEmail", t =>
        {
            t.HasCheckConstraint("CK_EnvioEmail_Estado", "[Estado] IN ('Pendente','Enviado','NaoEntregue')");
            t.HasCheckConstraint("CK_EnvioEmail_Reenvios", "[Reenvios] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_EnvioEmail_Origem", "[NotificacaoId] IS NOT NULL OR [ConviteId] IS NOT NULL");
            t.HasCheckConstraint(
                "CK_EnvioEmail_Coerencia",
                "([Estado] = 'Pendente' AND [ProximaTentativaEmUtc] IS NOT NULL) OR ([Estado] = 'Enviado' AND [ProximaTentativaEmUtc] IS NULL AND [UltimaTentativaEmUtc] IS NOT NULL) OR ([Estado] = 'NaoEntregue' AND [ProximaTentativaEmUtc] IS NULL AND [Reenvios] = 3)");
        });
        builder.HasKey(e => e.Id).HasName("PK_EnvioEmail");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.EnderecoDestino).HasMaxLength(254);
        builder.Property(e => e.Assunto).HasMaxLength(150);
        builder.Property(e => e.Estado).ComoTexto(12).EstadoPorOmissao(EstadoEnvioEmail.Pendente);
        builder.Property(e => e.Reenvios).HasDefaultValueSql("0").ValueGeneratedNever();
        builder.Property(e => e.CriadoEmUtc).InstanteUtcPorOmissao();
        builder.Property(e => e.PrimeiraTentativaEmUtc).InstanteUtc();
        builder.Property(e => e.UltimaTentativaEmUtc).InstanteUtc();
        builder.Property(e => e.ProximaTentativaEmUtc).InstanteUtc();
        builder.HasOne<Notificacao>()
            .WithMany()
            .HasForeignKey(e => e.NotificacaoId)
            .HasConstraintName("FK_EnvioEmail_NotificacaoId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Convite>()
            .WithMany()
            .HasForeignKey(e => e.ConviteId)
            .HasConstraintName("FK_EnvioEmail_ConviteId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.NotificacaoId, "UQ_EnvioEmail_Notificacao").IsUnique().HasFilter("[NotificacaoId] IS NOT NULL");
        builder.HasIndex(e => e.ConviteId, "UQ_EnvioEmail_Convite").IsUnique().HasFilter("[ConviteId] IS NOT NULL");
        builder.HasIndex(e => e.ProximaTentativaEmUtc, "IX_EnvioEmail_PendenteProxima").HasFilter("[Estado] = 'Pendente'");
    }
}
