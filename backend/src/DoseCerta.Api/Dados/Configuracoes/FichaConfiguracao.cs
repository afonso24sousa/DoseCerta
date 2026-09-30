// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Api.Dados.Configuracoes;

/// <summary>
/// Mapeamento da entidade <see cref="Ficha"/>, comum às fichas de terceiro e à ficha própria.
/// </summary>
public class FichaConfiguracao : IEntityTypeConfiguration<Ficha>
{
    /// <summary>
    /// Nome do índice que garante uma única ficha própria por Utilizador.
    /// </summary>
    public const string IndiceFichaPropriaUnica = "UX_Fichas_UtilizadorProprietarioId";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Ficha> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Fichas");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.NomeCompleto).HasMaxLength(200).IsRequired();
        builder.Property(f => f.DataNascimento).IsRequired();
        builder.Property(f => f.ContactoEmergenciaNome).HasMaxLength(200).IsRequired();
        builder.Property(f => f.ContactoEmergenciaTelefone).HasMaxLength(20).IsRequired();
        builder.Property(f => f.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.DataCriacao).IsRequired();
        builder.Ignore(f => f.EhFichaPropria);

        builder.HasOne(f => f.GrauAutonomia)
            .WithMany()
            .HasForeignKey(f => f.GrauAutonomiaId)
            .OnDelete(DeleteBehavior.Restrict);

        // F003: cada Utilizador tem no máximo uma ficha própria.
        builder.HasOne(f => f.UtilizadorProprietario)
            .WithMany()
            .HasForeignKey(f => f.UtilizadorProprietarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(f => f.UtilizadorProprietarioId)
            .IsUnique()
            .HasFilter("[UtilizadorProprietarioId] IS NOT NULL")
            .HasDatabaseName(IndiceFichaPropriaUnica);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Fichas_Estado",
            "[Estado] IN ('Ativa', 'Arquivada')"));

        builder.HasMany(f => f.Associacoes)
            .WithOne()
            .HasForeignKey(a => a.FichaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Associacoes).HasField("associacoes");

        builder.HasMany(f => f.Documentos)
            .WithOne()
            .HasForeignKey(d => d.FichaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Documentos).HasField("documentos");

        builder.HasMany(f => f.CondicoesSaude)
            .WithMany()
            .UsingEntity(
                "FichasCondicoesSaude",
                r => r.HasOne(typeof(CondicaoSaude)).WithMany().HasForeignKey("CondicaoSaudeId").OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne(typeof(Ficha)).WithMany().HasForeignKey("FichaId").OnDelete(DeleteBehavior.Cascade));
        builder.Navigation(f => f.CondicoesSaude).HasField("condicoesSaude");

        builder.HasMany(f => f.Alergias)
            .WithMany()
            .UsingEntity(
                "FichasAlergias",
                r => r.HasOne(typeof(Alergia)).WithMany().HasForeignKey("AlergiaId").OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne(typeof(Ficha)).WithMany().HasForeignKey("FichaId").OnDelete(DeleteBehavior.Cascade));
        builder.Navigation(f => f.Alergias).HasField("alergias");
    }
}
