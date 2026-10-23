// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das tomas (operações da geração e da remoção, usadas pela F005).
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
}
