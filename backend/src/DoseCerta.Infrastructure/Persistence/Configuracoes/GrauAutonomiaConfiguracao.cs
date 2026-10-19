// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>GrauAutonomia</c> (dicionário de dados, secção 6.5, e dados predefinidos da secção 8).
/// </summary>
internal sealed class GrauAutonomiaConfiguracao : IEntityTypeConfiguration<GrauAutonomia>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GrauAutonomia> builder)
    {
        builder.ToTable("GrauAutonomia");
        builder.HasKey(e => e.Id).HasName("PK_GrauAutonomia");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Designacao).HasMaxLength(80);
        builder.HasIndex(e => e.Designacao, "UQ_GrauAutonomia_Designacao").IsUnique();
        builder.HasData(DadosPredefinidos.GrausAutonomia());
    }
}
