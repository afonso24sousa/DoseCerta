// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Api.Dados.Configuracoes;

/// <summary>
/// Mapeamento da entidade <see cref="Utilizador"/> (contrato mínimo; a I001 completa-o).
/// </summary>
public class UtilizadorConfiguracao : IEntityTypeConfiguration<Utilizador>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Utilizador> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Utilizadores");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
