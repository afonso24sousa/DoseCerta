// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Api.Dados.Configuracoes;

/// <summary>
/// Mapeamento da entidade <see cref="DocumentoFicha"/>.
/// </summary>
public class DocumentoFichaConfiguracao : IEntityTypeConfiguration<DocumentoFicha>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DocumentoFicha> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Categoria fechada: receita médica, relatório clínico ou documento de identificação.
        builder.ToTable("DocumentosFicha", t => t.HasCheckConstraint(
            "CK_DocumentosFicha_Categoria",
            "[Categoria] IN ('ReceitaMedica', 'RelatorioClinico', 'DocumentoIdentificacao')"));

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Categoria).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(d => d.NomeOriginal).HasMaxLength(255).IsRequired();
        builder.Property(d => d.TipoConteudo).HasMaxLength(100).IsRequired();
        builder.Property(d => d.ChaveArmazenamento).HasMaxLength(500).IsRequired();
        builder.Property(d => d.DataCarregamento).IsRequired();
        builder.HasIndex(d => d.FichaId);
    }
}
