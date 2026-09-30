// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Api.Dados.Configuracoes;

/// <summary>
/// Mapeamento e dados das categorias fechadas (condições de saúde, alergias e graus de autonomia).
/// </summary>
public class CatalogosConfiguracao :
    IEntityTypeConfiguration<CondicaoSaude>,
    IEntityTypeConfiguration<Alergia>,
    IEntityTypeConfiguration<GrauAutonomia>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CondicaoSaude> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CondicoesSaude");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Nome).IsUnique();
        builder.HasData(DadosCatalogos.CondicoesSaude);
    }

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Alergia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Alergias");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Nome).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.Nome).IsUnique();
        builder.HasData(DadosCatalogos.Alergias);
    }

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GrauAutonomia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GrausAutonomia");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Nome).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => g.Nome).IsUnique();
        builder.HasData(DadosCatalogos.GrausAutonomia);
    }
}
