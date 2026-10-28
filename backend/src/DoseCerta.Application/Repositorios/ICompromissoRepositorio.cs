// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos compromissos (CompromissoRepositorio do Modelo de Classes do Backend). Nesta versão só tem a consulta usada pela
/// lista diária (F010); o registo, a edição, a remoção e os avisos são da F008 e da F011.
/// </summary>
public interface ICompromissoRepositorio
{
    /// <summary>
    /// Lista os compromissos de um conjunto de fichas num período, com o nome do utente de cada ficha (RF-100, RF-101).
    /// </summary>
    /// <param name="fichaIds">Fichas a considerar.</param>
    /// <param name="deUtc">Início do período, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do período, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos do período.</returns>
    Task<IReadOnlyList<CompromissoDoPeriodo>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento);
}
