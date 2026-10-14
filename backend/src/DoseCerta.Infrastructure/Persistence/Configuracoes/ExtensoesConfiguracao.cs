// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Convenções de mapeamento comuns às tabelas do modelo de dados (secção 3 e script T-SQL da secção 13.2).
/// Os valores predefinidos existem na base de dados (para os scripts e os dados iniciais), mas são sempre
/// enviados pela aplicação, que os define no domínio (<c>ValueGeneratedNever</c>).
/// </summary>
internal static class ExtensoesConfiguracao
{
    /// <summary>
    /// Collation das colunas de e-mail: comparação sem distinção de maiúsculas nem de acentos (RF-011).
    /// </summary>
    public const string CollationEmail = "Latin1_General_100_CI_AI";

    /// <summary>
    /// Mapeia um identificador <c>uniqueidentifier</c> com valor predefinido <c>NEWSEQUENTIALID()</c> (DM-01).
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<Guid> IdSequencial(this PropertyBuilder<Guid> propriedade)
    {
        return propriedade.HasDefaultValueSql("NEWSEQUENTIALID()").ValueGeneratedNever();
    }

    /// <summary>
    /// Mapeia um instante UTC para <c>datetime2(0)</c> (DM-15).
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<DateTime> InstanteUtc(this PropertyBuilder<DateTime> propriedade)
    {
        return propriedade.HasColumnType("datetime2(0)");
    }

    /// <summary>
    /// Mapeia um instante UTC opcional para <c>datetime2(0)</c> (DM-15).
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<DateTime?> InstanteUtc(this PropertyBuilder<DateTime?> propriedade)
    {
        return propriedade.HasColumnType("datetime2(0)");
    }

    /// <summary>
    /// Mapeia um instante UTC para <c>datetime2(0)</c> com valor predefinido <c>SYSUTCDATETIME()</c>.
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<DateTime> InstanteUtcPorOmissao(this PropertyBuilder<DateTime> propriedade)
    {
        return propriedade.HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedNever();
    }

    /// <summary>
    /// Mapeia uma hora fixa para <c>time(0)</c>.
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<TimeOnly> HoraLocal(this PropertyBuilder<TimeOnly> propriedade)
    {
        return propriedade.HasColumnType("time(0)");
    }

    /// <summary>
    /// Guarda um enumerado como texto <c>nvarchar</c> com o comprimento indicado (DM-06).
    /// </summary>
    /// <typeparam name="TEnum">Tipo do enumerado.</typeparam>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <param name="comprimento">Comprimento máximo do texto.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<TEnum> ComoTexto<TEnum>(this PropertyBuilder<TEnum> propriedade, int comprimento)
        where TEnum : struct, Enum
    {
        return propriedade.HasConversion<string>().HasMaxLength(comprimento);
    }

    /// <summary>
    /// Mapeia um valor lógico com valor predefinido na base de dados (<c>DEFAULT 0</c> ou <c>DEFAULT 1</c>, como no script).
    /// </summary>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <param name="valorPorOmissao">Valor predefinido.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<bool> LogicoPorOmissao(this PropertyBuilder<bool> propriedade, bool valorPorOmissao)
    {
        return propriedade.HasDefaultValueSql(valorPorOmissao ? "1" : "0").ValueGeneratedNever();
    }

    /// <summary>
    /// Mapeia um estado guardado como texto com valor predefinido na base de dados (<c>DEFAULT 'Valor'</c>, como no script).
    /// </summary>
    /// <typeparam name="TEnum">Tipo do enumerado.</typeparam>
    /// <param name="propriedade">Propriedade a configurar.</param>
    /// <param name="valorPorOmissao">Valor predefinido.</param>
    /// <returns>A mesma propriedade, para encadeamento.</returns>
    public static PropertyBuilder<TEnum> EstadoPorOmissao<TEnum>(this PropertyBuilder<TEnum> propriedade, TEnum valorPorOmissao)
        where TEnum : struct, Enum
    {
        return propriedade.HasDefaultValueSql($"'{valorPorOmissao}'").ValueGeneratedNever();
    }
}
