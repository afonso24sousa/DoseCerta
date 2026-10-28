// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos compromissos de saúde (F008). As consultas por intervalo usam <c>IX_Compromisso_FichaData</c>.
/// </summary>
public sealed class CompromissoRepositorio : ICompromissoRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissoRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public CompromissoRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<Compromisso?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Compromissos.FirstOrDefaultAsync(c => c.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Compromisso>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento)
    {
        var consulta = this.contexto.Compromissos.AsNoTracking().Where(c => c.FichaId == fichaId);
        if (deUtc is not null)
        {
            consulta = consulta.Where(c => c.DataHoraUtc >= deUtc);
        }

        if (ateUtc is not null)
        {
            consulta = consulta.Where(c => c.DataHoraUtc < ateUtc);
        }

        return await consulta
            .OrderBy(c => c.DataHoraUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Compromisso>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        return await this.contexto.Compromissos
            .AsNoTracking()
            .Where(c => fichaIds.Contains(c.FichaId) && c.DataHoraUtc >= deUtc && c.DataHoraUtc < ateUtc)
            .OrderBy(c => c.DataHoraUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Adicionar(Compromisso compromisso)
    {
        this.contexto.Compromissos.Add(compromisso);
    }

    /// <inheritdoc/>
    public void Remover(Compromisso compromisso)
    {
        this.contexto.Compromissos.Remove(compromisso);
    }
}
