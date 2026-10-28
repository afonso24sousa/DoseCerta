// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Consultas da lista diária (F010) em memória. Guarda as linhas já lidas, com o nome do utente e de quem confirmou, para os
/// testes poderem criar tomas confirmadas sem depender da confirmação (F006).
/// </summary>
public partial class RepositoriosEmMemoria : ICompromissoRepositorio
{
    /// <summary>
    /// Gets as tomas devolvidas à lista diária.
    /// </summary>
    public List<TomaDoDia> TomasDoDia { get; } = new();

    /// <summary>
    /// Gets os compromissos devolvidos à lista diária.
    /// </summary>
    public List<CompromissoDoPeriodo> CompromissosDoPeriodo { get; } = new();

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

    /// <inheritdoc/>
    public Task<IReadOnlyList<CompromissoDoPeriodo>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<CompromissoDoPeriodo> lista = this.CompromissosDoPeriodo
            .Where(c => fichaIds.Contains(c.FichaId) && c.DataHoraUtc >= deUtc && c.DataHoraUtc < ateUtc)
            .ToList();
        return Task.FromResult(lista);
    }
}
