// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às tomas (TomaRepositorio do Modelo de Classes do Backend). Nesta versão tem as operações da geração e da
/// remoção das tomas usadas pela F005 e a consulta da lista diária (F010); as restantes consultas e a confirmação são da F006.
/// </summary>
public interface ITomaRepositorio
{
    /// <summary>
    /// Regista uma toma prevista, se ainda não existir outra do mesmo tratamento no mesmo instante; torna a geração
    /// idempotente (RF-060, <c>UQ_Toma_TratamentoInstante</c>).
    /// </summary>
    /// <param name="toma">Toma prevista.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se a toma foi registada.</returns>
    Task<bool> AdicionarSeNaoExisteAsync(Toma toma, CancellationToken cancelamento);

    /// <summary>
    /// Apaga as tomas pendentes de um tratamento previstas depois de um instante; as anteriores e as já confirmadas
    /// mantêm-se (RF-055, RF-056, RF-060).
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="aPartirDeUtc">Instante a partir do qual se apagam, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas apagadas.</returns>
    Task<int> EliminarFuturasPendentesAsync(Guid tratamentoId, DateTime aPartirDeUtc, CancellationToken cancelamento);

    /// <summary>
    /// Lista as tomas de um conjunto de fichas previstas num período, com o nome do utente e o nome de quem confirmou, para a
    /// lista diária dos Cuidadores (RF-100, RF-101, RF-104).
    /// </summary>
    /// <param name="fichaIds">Fichas a considerar.</param>
    /// <param name="deUtc">Início do período, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do período, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas do período.</returns>
    Task<IReadOnlyList<TomaDoDia>> DoDiaDosCuidadoresAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento);
}
