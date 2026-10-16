// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Documento</c> (dicionário de dados, secção 6.13).
/// </summary>
internal sealed class DocumentoConfiguracao : IEntityTypeConfiguration<Documento>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("Documento", t =>
        {
            t.HasCheckConstraint("CK_Documento_Categoria", "[Categoria] IN ('ReceitaMedica','RelatorioClinico','DocumentoIdentificacao')");
            t.HasCheckConstraint("CK_Documento_Formato", "[Formato] IN ('PDF','JPG','PNG')");
            t.HasCheckConstraint("CK_Documento_Tamanho", "[TamanhoBytes] BETWEEN 1 AND 10485760");
            t.HasCheckConstraint("CK_Documento_Receita", "[TratamentoId] IS NULL OR [Categoria] = 'ReceitaMedica'");
        });
        builder.HasKey(e => e.Id).HasName("PK_Documento");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.Categoria).ComoTexto(25);
        builder.Property(e => e.NomeOriginal).HasMaxLength(255);
        builder.Property(e => e.CaminhoRelativo).HasMaxLength(400);
        builder.Property(e => e.Formato).ComoTexto(3);
        builder.Property(e => e.CarregadoEmUtc).InstanteUtcPorOmissao();
        builder.HasOne<Ficha>()
            .WithMany()
            .HasForeignKey(e => e.FichaId)
            .HasConstraintName("FK_Documento_FichaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Tratamento>()
            .WithMany()
            .HasForeignKey(e => e.TratamentoId)
            .HasConstraintName("FK_Documento_TratamentoId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.CarregadoPorId)
            .HasConstraintName("FK_Documento_CarregadoPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => e.CaminhoRelativo, "UQ_Documento_Caminho").IsUnique();
        builder.HasIndex(e => new { e.FichaId, e.Categoria }, "IX_Documento_Ficha");
    }
}
