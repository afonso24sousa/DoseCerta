// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das tomas (operações da geração e da remoção, usadas pela F005, e consulta da lista diária, F010).
/// </summary>
public sealed class TomaRepositorio : ITomaRepositorio
{
    private readonly DoseCertaDbContext contexto;

    // Instantes já existentes por tratamento, lidos uma vez por pedido para não consultar a base de dados por cada toma.
    private readonly Dictionary<Guid, HashSet<DateTime>> instantes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TomaRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public TomaRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public async Task<bool> AdicionarSeNaoExisteAsync(Toma toma, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(toma);

        if (!this.instantes.TryGetValue(toma.TratamentoId, out var existentes))
        {
            var lidos = await this.contexto.Tomas
                .Where(t => t.TratamentoId == toma.TratamentoId)
                .Select(t => t.PrevistaEmUtc)
                .ToListAsync(cancelamento)
                .ConfigureAwait(false);
            existentes = lidos.ToHashSet();
            this.instantes[toma.TratamentoId] = existentes;
        }

        if (!existentes.Add(toma.PrevistaEmUtc))
        {
            return false;
        }

        this.contexto.Tomas.Add(toma);
        return true;
    }

    /// <inheritdoc/>
    public Task<int> EliminarFuturasPendentesAsync(Guid tratamentoId, DateTime aPartirDeUtc, CancellationToken cancelamento)
    {
        this.instantes.Remove(tratamentoId);
        return this.contexto.Tomas
            .Where(t => t.TratamentoId == tratamentoId && t.Estado == EstadoToma.Pendente && t.PrevistaEmUtc > aPartirDeUtc)
            .ExecuteDeleteAsync(cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TomaDoDia>> DoDiaDosCuidadoresAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        return await this.contexto.Tomas
            .AsNoTracking()
            .Where(t => t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc)
            .Join(this.contexto.Tratamentos, t => t.TratamentoId, tr => tr.Id, (t, tr) => new { Toma = t, tr.FichaId })
            .Where(p => fichaIds.Contains(p.FichaId))
            .Join(this.contexto.Fichas, p => p.FichaId, f => f.Id, (p, f) => new { p.Toma, Ficha = f })
            .Select(p => new TomaDoDia(
                p.Toma.Id,
                p.Ficha.Id,
                p.Ficha.NomeCompleto,
                p.Toma.Medicamento,
                p.Toma.Dose,
                p.Toma.PrevistaEmUtc,
                p.Toma.Estado,
                this.contexto.Utilizadores.Where(u => u.Id == p.Toma.ConfirmadaPorId).Select(u => u.Nome).FirstOrDefault(),
                p.Toma.ConfirmadaEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }
}
