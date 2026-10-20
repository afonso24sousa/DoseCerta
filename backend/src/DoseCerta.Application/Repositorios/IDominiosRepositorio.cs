// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às listas fechadas de necessidades específicas (DominiosRepositorio do Modelo de Classes do Backend, RF-021).
/// </summary>
public interface IDominiosRepositorio
{
    /// <summary>
    /// Lista as condições de saúde.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As condições de saúde, por identificador.</returns>
    Task<IReadOnlyList<CondicaoSaude>> ListarCondicoesAsync(CancellationToken cancelamento);

    /// <summary>
    /// Lista as alergias.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As alergias, por identificador.</returns>
    Task<IReadOnlyList<Alergia>> ListarAlergiasAsync(CancellationToken cancelamento);

    /// <summary>
    /// Lista os graus de autonomia ou mobilidade.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os graus, pela ordem de apresentação.</returns>
    Task<IReadOnlyList<GrauAutonomia>> ListarGrausAutonomiaAsync(CancellationToken cancelamento);

    /// <summary>
    /// Obtém as condições de saúde com os identificadores indicados.
    /// </summary>
    /// <param name="ids">Identificadores pedidos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As condições existentes; os identificadores fora da lista não são devolvidos.</returns>
    Task<IReadOnlyList<CondicaoSaude>> CondicoesPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento);

    /// <summary>
    /// Obtém as alergias com os identificadores indicados.
    /// </summary>
    /// <param name="ids">Identificadores pedidos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As alergias existentes; os identificadores fora da lista não são devolvidos.</returns>
    Task<IReadOnlyList<Alergia>> AlergiasPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento);

    /// <summary>
    /// Obtém um grau de autonomia ou mobilidade.
    /// </summary>
    /// <param name="id">Identificador do grau.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O grau, ou <see langword="null"/> se não pertencer à lista.</returns>
    Task<GrauAutonomia?> GrauPorIdAsync(short id, CancellationToken cancelamento);
}
