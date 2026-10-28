// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Ocorrencias;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às notas partilhadas (NotaRepositorio do Modelo de Classes do Backend). Não há operações de alteração nem de
/// eliminação: as notas são imutáveis (RF-091, RNF-049).
/// </summary>
public interface INotaRepositorio
{
    /// <summary>
    /// Lista as notas de uma ficha com o nome do autor, da mais recente para a mais antiga pelo instante do registo (RF-095).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="deUtc">Limite inferior do instante do registo, inclusivo, ou <see langword="null"/>.</param>
    /// <param name="ateUtc">Limite superior do instante do registo, exclusivo, ou <see langword="null"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notas da ficha.</returns>
    Task<IReadOnlyList<RegistoPartilhado>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento);

    /// <summary>
    /// Acrescenta uma nota nova, a gravar na transação em curso.
    /// </summary>
    /// <param name="nota">Nota a registar.</param>
    void Adicionar(Nota nota);
}
