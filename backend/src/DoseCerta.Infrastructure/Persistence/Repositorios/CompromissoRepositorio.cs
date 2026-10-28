// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos compromissos (consulta da lista diária, F010).
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
    public async Task<IReadOnlyList<CompromissoDoPeriodo>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        return await this.contexto.Compromissos
            .AsNoTracking()
            .Where(c => fichaIds.Contains(c.FichaId) && c.DataHoraUtc >= deUtc && c.DataHoraUtc < ateUtc)
            .Join(this.contexto.Fichas, c => c.FichaId, f => f.Id, (c, f) => new CompromissoDoPeriodo(
                c.Id,
                f.Id,
                f.NomeCompleto,
                c.Motivo,
                c.DataHoraUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }
}
