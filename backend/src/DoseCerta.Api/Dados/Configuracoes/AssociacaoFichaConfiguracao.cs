// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Api.Dados.Configuracoes;

/// <summary>
/// Mapeamento da entidade <see cref="AssociacaoFicha"/>.
/// </summary>
public class AssociacaoFichaConfiguracao : IEntityTypeConfiguration<AssociacaoFicha>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AssociacaoFicha> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AssociacoesFicha", t =>
        {
            t.HasCheckConstraint("CK_AssociacoesFicha_Papel", "[Papel] IN ('Cuidador', 'Familiar')");
            t.HasCheckConstraint(
                "CK_AssociacoesFicha_PrincipalEhCuidador",
                "[EhCuidadorPrincipal] = 0 OR [Papel] = 'Cuidador'");
        });

        builder.HasKey(a => new { a.FichaId, a.UtilizadorId });
        builder.Property(a => a.Papel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.DataAssociacao).IsRequired();

        builder.HasOne(a => a.Utilizador)
            .WithMany()
            .HasForeignKey(a => a.UtilizadorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cada ficha tem no máximo um Cuidador principal.
        builder.HasIndex(a => a.FichaId)
            .IsUnique()
            .HasFilter("[EhCuidadorPrincipal] = 1")
            .HasDatabaseName("UX_AssociacoesFicha_CuidadorPrincipal");

        builder.HasIndex(a => a.UtilizadorId);
    }
}
