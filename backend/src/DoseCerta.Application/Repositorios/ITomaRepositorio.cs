// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às tomas (TomaRepositorio do Modelo de Classes do Backend). Nesta versão só tem as operações da geração e da
/// remoção das tomas usadas pela F005; as consultas e a confirmação são da F006.
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
}
