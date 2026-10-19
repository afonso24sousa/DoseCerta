// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Utilizador</c> (dicionário de dados, secção 6.1).
/// </summary>
internal sealed class UtilizadorConfiguracao : IEntityTypeConfiguration<Utilizador>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Utilizador> builder)
    {
        builder.ToTable("Utilizador");
        builder.HasKey(e => e.Id).HasName("PK_Utilizador");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Nome).HasMaxLength(120);
        builder.Property(e => e.Email).HasMaxLength(254).UseCollation(ExtensoesConfiguracao.CollationEmail);
        builder.Property(e => e.PalavraPasseHash).HasMaxLength(255);
        builder.Property(e => e.CriadoEmUtc).InstanteUtcPorOmissao();
        builder.HasIndex(e => e.Email, "UQ_Utilizador_Email").IsUnique();
    }
}
