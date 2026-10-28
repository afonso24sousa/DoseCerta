// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos compromissos de saúde (CompromissoRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface ICompromissoRepositorio
{
    /// <summary>
    /// Obtém um compromisso para consulta ou alteração.
    /// </summary>
    /// <param name="id">Identificador do compromisso.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O compromisso, ou <see langword="null"/> se não existir.</returns>
    Task<Compromisso?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Lista os compromissos de uma ficha, opcionalmente num intervalo, por ordem da data e hora (OP-42).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC; <see langword="null"/> sem limite.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC; <see langword="null"/> sem limite.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos da ficha.</returns>
    Task<IReadOnlyList<Compromisso>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento);

    /// <summary>
    /// Lista os compromissos de várias fichas num intervalo, numa só consulta, por ordem da data e hora; usado pela agenda
    /// (OP-45) e pela lista diária (OP-50).
    /// </summary>
    /// <param name="fichaIds">Fichas pedidas.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos das fichas no intervalo.</returns>
    Task<IReadOnlyList<Compromisso>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento);

    /// <summary>
    /// Regista um compromisso novo.
    /// </summary>
    /// <param name="compromisso">Compromisso a registar.</param>
    void Adicionar(Compromisso compromisso);

    /// <summary>
    /// Remove um compromisso; o aviso deixa de ser emitido (RF-082).
    /// </summary>
    /// <param name="compromisso">Compromisso a remover.</param>
    void Remover(Compromisso compromisso);
}
