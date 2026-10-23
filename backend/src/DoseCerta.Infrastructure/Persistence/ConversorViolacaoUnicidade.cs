// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Converte a violação de um índice único que garante uma regra de negócio na <see cref="DominioException"/> dessa regra.
/// Cobre os pedidos simultâneos que passam ambos a verificação prévia do serviço (RF-031, RF-043, RNF-046). A violação de
/// <c>UQ_Associacao_Principal</c> (duas transferências do Cuidador principal em simultâneo) não é convertida: a documentação da API
/// não define o erro; o índice garante que a ficha nunca fica com dois Cuidadores principais (limitação conhecida, D8).
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

    /// <summary>
    /// Nome do índice que limita a um convite pendente por e-mail e por ficha (<c>ConviteConfiguracao</c>).
    /// </summary>
    internal const string IndiceConvitePendente = "UQ_Convite_Pendente";

    /// <summary>
    /// Nome do índice que limita a uma associação por Utilizador e por ficha (<c>AssociacaoConfiguracao</c>).
    /// </summary>
    internal const string IndiceAssociacaoFichaUtilizador = "UQ_Associacao_FichaUtilizador";

    private static readonly Dictionary<string, (string Codigo, string Mensagem)> RegrasPorIndice = new(StringComparer.Ordinal)
    {
        [IndiceFichaPropria] = (CodigosErro.FichaPropriaJaExiste, FichaService.MensagemFichaPropriaJaExiste),
        [IndiceConvitePendente] = (CodigosErro.ConvitePendenteExiste, ConviteService.MensagemConvitePendenteExiste),
        [IndiceAssociacaoFichaUtilizador] = (CodigosErro.PessoaJaAssociada, Ficha.MensagemPessoaJaAssociada),
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
