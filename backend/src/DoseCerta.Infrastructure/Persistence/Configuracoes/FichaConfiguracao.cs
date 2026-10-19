// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Ficha</c> (dicionário de dados, secções 6.6 a 6.8).
/// </summary>
internal sealed class FichaConfiguracao : IEntityTypeConfiguration<Ficha>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Ficha> builder)
    {
        builder.ToTable("Ficha", t =>
        {
            t.HasCheckConstraint("CK_Ficha_Estado", "[Estado] IN ('Ativa','Arquivada')");
            t.HasCheckConstraint(
                "CK_Ficha_Tipo",
                "([Tipo] = 'Propria' AND [UtilizadorProprioId] IS NOT NULL) OR ([Tipo] = 'Terceira' AND [UtilizadorProprioId] IS NULL)");
            t.HasCheckConstraint("CK_Ficha_Nascimento", "[DataNascimento] >= '1900-01-01'");
            t.HasCheckConstraint(
                "CK_Ficha_Arquivo",
                "([Estado] = 'Arquivada' AND [ArquivadaEmUtc] IS NOT NULL AND [ArquivadaEmUtc] >= [AtivaDesdeUtc]) OR ([Estado] = 'Ativa' AND [ArquivadaEmUtc] IS NULL)");
        });
        builder.HasKey(e => e.Id).HasName("PK_Ficha");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.NomeCompleto).HasMaxLength(150);
        builder.Property(e => e.DataNascimento).HasColumnType("date");
        builder.Property(e => e.ContactoEmergencia).HasMaxLength(30);
        builder.Property(e => e.Tipo).ComoTexto(10);
        builder.Property(e => e.Estado).ComoTexto(10).EstadoPorOmissao(EstadoFicha.Ativa);
        builder.Property(e => e.CriadaEmUtc).InstanteUtcPorOmissao();
        builder.Property(e => e.AtivaDesdeUtc).InstanteUtcPorOmissao();
        builder.Property(e => e.ArquivadaEmUtc).InstanteUtc();

        builder.HasOne(e => e.GrauAutonomia)
            .WithMany()
            .HasForeignKey(e => e.GrauAutonomiaId)
            .HasConstraintName("FK_Ficha_GrauAutonomiaId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.UtilizadorProprioId)
            .HasConstraintName("FK_Ficha_UtilizadorProprioId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.CriadaPorId)
            .HasConstraintName("FK_Ficha_CriadaPorId")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.Associacoes)
            .WithOne()
            .HasForeignKey(a => a.FichaId)
            .HasConstraintName("FK_Associacao_FichaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Associacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.CondicoesSaude)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "FichaCondicaoSaude",
                direita => direita.HasOne<CondicaoSaude>()
                    .WithMany()
                    .HasForeignKey("CondicaoSaudeId")
                    .HasConstraintName("FK_FichaCondicaoSaude_CondicaoSaudeId")
                    .OnDelete(DeleteBehavior.NoAction),
                esquerda => esquerda.HasOne<Ficha>()
                    .WithMany()
                    .HasForeignKey("FichaId")
                    .HasConstraintName("FK_FichaCondicaoSaude_FichaId")
                    .OnDelete(DeleteBehavior.Cascade),
                juncao =>
                {
                    juncao.ToTable("FichaCondicaoSaude");
                    juncao.HasKey("FichaId", "CondicaoSaudeId").HasName("PK_FichaCondicaoSaude");
                });
        builder.Navigation(e => e.CondicoesSaude).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Alergias)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "FichaAlergia",
                direita => direita.HasOne<Alergia>()
                    .WithMany()
                    .HasForeignKey("AlergiaId")
                    .HasConstraintName("FK_FichaAlergia_AlergiaId")
                    .OnDelete(DeleteBehavior.NoAction),
                esquerda => esquerda.HasOne<Ficha>()
                    .WithMany()
                    .HasForeignKey("FichaId")
                    .HasConstraintName("FK_FichaAlergia_FichaId")
                    .OnDelete(DeleteBehavior.Cascade),
                juncao =>
                {
                    juncao.ToTable("FichaAlergia");
                    juncao.HasKey("FichaId", "AlergiaId").HasName("PK_FichaAlergia");
                });
        builder.Navigation(e => e.Alergias).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.UtilizadorProprioId, "UQ_Ficha_UtilizadorProprio")
            .IsUnique()
            .HasFilter("[UtilizadorProprioId] IS NOT NULL");
        builder.HasIndex(e => e.Estado, "IX_Ficha_Estado");
    }
}
