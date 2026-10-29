// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Tomas;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Consulta das tomas da lista diária (F010) em memória. Guarda as linhas já lidas, com o nome do utente e de quem confirmou,
/// para os testes poderem criar tomas em qualquer estado sem passar pela confirmação (F006).
/// </summary>
public partial class RepositoriosEmMemoria
{
    /// <summary>
    /// Gets as tomas devolvidas à lista diária.
    /// </summary>
    public List<TomaDoDia> TomasDoDia { get; } = new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<TomaDoDia>> DoDiaDosCuidadoresAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<TomaDoDia> lista = this.TomasDoDia
            .Where(t => fichaIds.Contains(t.FichaId) && t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc)
            .ToList();
        return Task.FromResult(lista);
    }
}
