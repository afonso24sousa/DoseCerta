// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Mapeamento da tabela <c>Toma</c> (dicionário de dados, secção 6.14, e regras da secção 7.1).
/// </summary>
internal sealed class TomaConfiguracao : IEntityTypeConfiguration<Toma>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Toma> builder)
    {
        builder.ToTable("Toma", t =>
        {
            t.HasCheckConstraint("CK_Toma_Estado", "[Estado] IN ('Pendente','Confirmada','ConfirmadaComAtraso','NaoConfirmada')");
            t.HasCheckConstraint("CK_Toma_Grau", "[GrauImportancia] IN ('Leve','Moderado','Grave')");
            t.HasCheckConstraint(
                "CK_Toma_Confirmacao",
                "([Estado] IN ('Confirmada','ConfirmadaComAtraso') AND [ConfirmadaPorId] IS NOT NULL AND [ConfirmadaEmUtc] IS NOT NULL AND [NaoConfirmadaDetetadaEmUtc] IS NULL) OR ([Estado] = 'Pendente' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL AND [NaoConfirmadaDetetadaEmUtc] IS NULL) OR ([Estado] = 'NaoConfirmada' AND [ConfirmadaPorId] IS NULL AND [ConfirmadaEmUtc] IS NULL AND [NaoConfirmadaDetetadaEmUtc] IS NOT NULL)");
            t.HasCheckConstraint(
                "CK_Toma_Limite24h",
                "([ConfirmadaEmUtc] IS NULL OR [ConfirmadaEmUtc] < DATEADD(HOUR, 24, [PrevistaEmUtc])) AND ([NaoConfirmadaDetetadaEmUtc] IS NULL OR [NaoConfirmadaDetetadaEmUtc] >= DATEADD(HOUR, 24, [PrevistaEmUtc]))");
            t.HasCheckConstraint(
                "CK_Toma_Tolerancia",
                "([Estado] <> 'Confirmada' OR [ConfirmadaEmUtc] <= DATEADD(MINUTE, CASE [GrauImportancia] WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, [PrevistaEmUtc])) AND ([Estado] <> 'ConfirmadaComAtraso' OR [ConfirmadaEmUtc] > DATEADD(MINUTE, CASE [GrauImportancia] WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, [PrevistaEmUtc]))");
            t.HasCheckConstraint("CK_Toma_Familiares", "[FamiliaresNotificadosEmUtc] IS NULL OR [GrauImportancia] = 'Grave'");
            t.HasTrigger(GatilhosSql.NomeGatilhoToma);
        });
        builder.HasKey(e => e.Id).HasName("PK_Toma");
        builder.Property(e => e.Id).IdSequencial();
        builder.Property(e => e.PrevistaEmUtc).InstanteUtc();
        builder.Property(e => e.Medicamento).HasMaxLength(120);
        builder.Property(e => e.Dose).HasMaxLength(60);
        builder.Property(e => e.GrauImportancia).ComoTexto(10);
        builder.Property(e => e.Estado).ComoTexto(20).EstadoPorOmissao(EstadoToma.Pendente);
        builder.Property(e => e.ConfirmadaEmUtc).InstanteUtc();
        builder.Property(e => e.NaoConfirmadaDetetadaEmUtc).InstanteUtc();
        builder.Property(e => e.CuidadoresNotificadosEmUtc).InstanteUtc();
        builder.Property(e => e.FamiliaresNotificadosEmUtc).InstanteUtc();
        builder.Property(e => e.Versao).IsRowVersion();
        builder.HasOne<Tratamento>()
            .WithMany()
            .HasForeignKey(e => e.TratamentoId)
            .HasConstraintName("FK_Toma_TratamentoId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Utilizador>()
            .WithMany()
            .HasForeignKey(e => e.ConfirmadaPorId)
            .HasConstraintName("FK_Toma_ConfirmadaPorId")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.TratamentoId, e.PrevistaEmUtc }, "UQ_Toma_TratamentoInstante").IsUnique();
        builder.HasIndex(e => e.PrevistaEmUtc, "IX_Toma_PendentePrevista").HasFilter("[Estado] = 'Pendente'");
    }
}
