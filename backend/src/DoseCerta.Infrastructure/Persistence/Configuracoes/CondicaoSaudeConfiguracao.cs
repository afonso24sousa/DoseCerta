// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>CondicaoSaude</c> (dicionário de dados, secção 6.3, e dados predefinidos da secção 8).
/// </summary>
internal sealed class CondicaoSaudeConfiguracao : IEntityTypeConfiguration<CondicaoSaude>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CondicaoSaude> builder)
    {
        builder.ToTable("CondicaoSaude");
        builder.HasKey(e => e.Id).HasName("PK_CondicaoSaude");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Designacao).HasMaxLength(80);
        builder.HasIndex(e => e.Designacao, "UQ_CondicaoSaude_Designacao").IsUnique();
        builder.HasData(DadosPredefinidos.CondicoesSaude());
    }
}
