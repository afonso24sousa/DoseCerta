// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às tomas (TomaRepositorio do Modelo de Classes do Backend): geração e remoção (F005), consulta, confirmação,
/// calendário e passagem a não confirmada (F006).
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
    /// Obtém uma toma para a confirmar.
    /// </summary>
    /// <param name="tomaId">Identificador da toma.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A toma, ou <see langword="null"/> se não existir.</returns>
    Task<Toma?> ObterAsync(Guid tomaId, CancellationToken cancelamento);

    /// <summary>
    /// Lista as tomas de uma ficha previstas num intervalo, com o nome de quem confirmou (OP-32, RF-063).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC.</param>
    /// <param name="estado">Filtro opcional pelo estado.</param>
    /// <param name="tratamentoId">Filtro opcional pelo tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas, por ordem do instante previsto.</returns>
    Task<IReadOnlyList<TomaDaFicha>> DoPeriodoAsync(
        Guid fichaId,
        DateTime deUtc,
        DateTime ateUtc,
        EstadoToma? estado,
        Guid? tratamentoId,
        CancellationToken cancelamento);

    /// <summary>
    /// Lista as tomas pendentes das próximas 48 horas das fichas ativas em que o Utilizador é Cuidador (OP-34, RF-061, RF-062).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas com <c>agora &lt;= prevista &lt; agora + 48 h</c>, por ordem do instante previsto.</returns>
    Task<IReadOnlyList<TomaParaSincronizar>> PendentesProximas48hAsync(Guid utilizadorId, DateTime agoraUtc, CancellationToken cancelamento);

    /// <summary>
    /// Obtém, para alterar, as tomas pendentes com 24 horas ou mais desde a hora prevista (RF-068, TR-06).
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas que devem passar a não confirmadas.</returns>
    Task<IReadOnlyList<Toma>> PendentesComVinteEQuatroHorasAsync(DateTime agoraUtc, CancellationToken cancelamento);

    /// <summary>
    /// Lista as tomas de várias fichas previstas num intervalo, numa só consulta, com a ficha de cada uma (OP-45).
    /// </summary>
    /// <param name="fichaIds">Fichas pedidas.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas das fichas no intervalo, por ordem do instante previsto.</returns>
    Task<IReadOnlyList<TomaNaFicha>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento);
}
