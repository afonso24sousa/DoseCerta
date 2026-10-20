// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das listas fechadas de condições de saúde, alergias e graus de autonomia (RF-021).
/// </summary>
public sealed class DominiosRepositorio : IDominiosRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="DominiosRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public DominiosRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CondicaoSaude>> ListarCondicoesAsync(CancellationToken cancelamento)
    {
        return await this.contexto.CondicoesSaude.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Alergia>> ListarAlergiasAsync(CancellationToken cancelamento)
    {
        return await this.contexto.Alergias.AsNoTracking().OrderBy(a => a.Id).ToListAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GrauAutonomia>> ListarGrausAutonomiaAsync(CancellationToken cancelamento)
    {
        return await this.contexto.GrausAutonomia.AsNoTracking().OrderBy(g => g.Ordem).ToListAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CondicaoSaude>> CondicoesPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<CondicaoSaude>();
        }

        return await this.contexto.CondicoesSaude.Where(c => ids.Contains(c.Id)).OrderBy(c => c.Id).ToListAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Alergia>> AlergiasPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Alergia>();
        }

        return await this.contexto.Alergias.Where(a => ids.Contains(a.Id)).OrderBy(a => a.Id).ToListAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<GrauAutonomia?> GrauPorIdAsync(short id, CancellationToken cancelamento)
    {
        return this.contexto.GrausAutonomia.FirstOrDefaultAsync(g => g.Id == id, cancelamento);
    }
}
