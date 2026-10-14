// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>SessaoRevogada</c> (dicionário de dados, secção 6.2).
/// </summary>
internal sealed class SessaoRevogadaConfiguracao : IEntityTypeConfiguration<SessaoRevogada>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SessaoRevogada> builder)
    {
        builder.ToTable("SessaoRevogada", t => t.HasCheckConstraint(
            "CK_SessaoRevogada_Validade",
            "[ExpiraEmUtc] <= DATEADD(HOUR, 8, [RevogadaEmUtc])"));
        builder.HasKey(e => e.Jti).HasName("PK_SessaoRevogada");
        builder.Property(e => e.Jti).ValueGeneratedNever();
        builder.Property(e => e.RevogadaEmUtc).InstanteUtcPorOmissao();
        builder.Property(e => e.ExpiraEmUtc).InstanteUtc();
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.UtilizadorId)
            .HasConstraintName("FK_SessaoRevogada_UtilizadorId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ExpiraEmUtc, "IX_SessaoRevogada_ExpiraEmUtc");
    }
}
