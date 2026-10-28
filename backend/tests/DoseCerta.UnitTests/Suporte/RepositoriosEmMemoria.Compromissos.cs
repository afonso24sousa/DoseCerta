// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositório em memória dos compromissos (F008).
/// </summary>
public partial class RepositoriosEmMemoria : ICompromissoRepositorio
{
    /// <summary>
    /// Gets os compromissos registados.
    /// </summary>
    public List<Compromisso> Compromissos { get; } = new();

    /// <inheritdoc/>
    Task<Compromisso?> ICompromissoRepositorio.ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Compromissos.FirstOrDefault(c => c.Id == id));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Compromisso>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<Compromisso> lista = this.Compromissos
            .Where(c => c.FichaId == fichaId && (deUtc is null || c.DataHoraUtc >= deUtc) && (ateUtc is null || c.DataHoraUtc < ateUtc))
            .OrderBy(c => c.DataHoraUtc)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    Task<IReadOnlyList<Compromisso>> ICompromissoRepositorio.DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<Compromisso> lista = this.Compromissos
            .Where(c => fichaIds.Contains(c.FichaId) && c.DataHoraUtc >= deUtc && c.DataHoraUtc < ateUtc)
            .OrderBy(c => c.DataHoraUtc)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public void Adicionar(Compromisso compromisso)
    {
        this.Compromissos.Add(compromisso);
    }

    /// <inheritdoc/>
    public void Remover(Compromisso compromisso)
    {
        this.Compromissos.Remove(compromisso);
    }
}
