// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Converte a violação de um índice único que garante uma regra de negócio na <see cref="DominioException"/> dessa regra.
/// Cobre os pedidos simultâneos que passam ambos a verificação prévia do serviço (RF-031, RNF-046).
/// </summary>
internal static class ConversorViolacaoUnicidade
{
    /// <summary>
    /// Número do erro do SQL Server para uma linha duplicada num índice único.
    /// </summary>
    internal const int ErroIndiceUnico = 2601;

    /// <summary>
    /// Número do erro do SQL Server para uma violação de uma restrição UNIQUE.
    /// </summary>
    internal const int ErroRestricaoUnica = 2627;

    /// <summary>
    /// Nome do índice que limita a uma ficha própria por Utilizador (<c>FichaConfiguracao</c>).
    /// </summary>
    internal const string IndiceFichaPropria = "UQ_Ficha_UtilizadorProprio";

    private static readonly Dictionary<string, (string Codigo, string Mensagem)> RegrasPorIndice = new(StringComparer.Ordinal)
    {
        [IndiceFichaPropria] = (CodigosErro.FichaPropriaJaExiste, FichaService.MensagemFichaPropriaJaExiste),
    };

    /// <summary>
    /// Converte uma falha de gravação causada por um índice único conhecido no erro de negócio correspondente.
    /// </summary>
    /// <param name="erro">Falha de gravação do EF Core.</param>
    /// <returns>O erro de negócio, ou <see langword="null"/> se a falha não for uma violação de um índice conhecido.</returns>
    public static DominioException? Converter(DbUpdateException erro)
    {
        ArgumentNullException.ThrowIfNull(erro);
        return erro.InnerException is SqlException { Number: ErroIndiceUnico or ErroRestricaoUnica } sql
            ? DaMensagem(sql.Message)
            : null;
    }

    /// <summary>
    /// Procura na mensagem do SQL Server o nome de um índice conhecido.
    /// </summary>
    /// <param name="mensagemSql">Mensagem do erro de unicidade do SQL Server.</param>
    /// <returns>O erro de negócio, ou <see langword="null"/> se nenhum índice conhecido for indicado.</returns>
    internal static DominioException? DaMensagem(string mensagemSql)
    {
        foreach (var (indice, regra) in RegrasPorIndice)
        {
            if (mensagemSql.Contains($"'{indice}'", StringComparison.Ordinal))
            {
                return new DominioException(regra.Codigo, regra.Mensagem);
            }
        }

        return null;
    }
}
