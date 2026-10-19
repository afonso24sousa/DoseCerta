// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Nota</c> (dicionário de dados, secção 6.21).
/// </summary>
internal sealed class NotaConfiguracao : IEntityTypeConfiguration<Nota>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Nota> builder)
    {
        builder.ToTable("Nota", t => t.HasTrigger(GatilhosSql.NomeGatilhoNota));
        builder.HasKey(e => e.Id).HasName("PK_Nota");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Texto).HasMaxLength(2000);
        builder.Property(e => e.RegistadaEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Nota_FichaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.AutorId)
            .HasConstraintName("FK_Nota_AutorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.FichaId, e.RegistadaEmUtc }, "IX_Nota_FichaRegisto").IsDescending(false, true);
    }
}
