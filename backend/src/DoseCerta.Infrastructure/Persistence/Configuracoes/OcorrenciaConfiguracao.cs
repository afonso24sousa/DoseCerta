// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Ocorrencia</c> (dicionário de dados, secção 6.20).
/// </summary>
internal sealed class OcorrenciaConfiguracao : IEntityTypeConfiguration<Ocorrencia>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Ocorrencia> builder)
    {
        builder.ToTable("Ocorrencia", t => t.HasTrigger(GatilhosSql.NomeGatilhoOcorrencia));
        builder.HasKey(e => e.Id).HasName("PK_Ocorrencia");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Descricao).HasMaxLength(2000);
        builder.Property(e => e.OcorreuEmUtc).InstanteUtc();
        builder.Property(e => e.RegistadaEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Ocorrencia_FichaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.AutorId)
            .HasConstraintName("FK_Ocorrencia_AutorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.RegistadaEmUtc }, "IX_Ocorrencia_FichaRegisto").IsDescending(false, true);
    }
}
