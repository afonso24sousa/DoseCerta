// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Alergia</c> (dicionário de dados, secção 6.4, e dados predefinidos da secção 8).
/// </summary>
internal sealed class AlergiaConfiguracao : IEntityTypeConfiguration<Alergia>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Alergia> builder)
    {
        builder.ToTable("Alergia");
        builder.HasKey(e => e.Id).HasName("PK_Alergia");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Designacao).HasMaxLength(80);
        builder.HasIndex(e => e.Designacao, "UQ_Alergia_Designacao").IsUnique();
        builder.HasData(DadosPredefinidos.Alergias());
    }
}
