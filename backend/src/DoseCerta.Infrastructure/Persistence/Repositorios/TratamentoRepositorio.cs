// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos tratamentos.
/// </summary>
public sealed class TratamentoRepositorio : ITratamentoRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratamentoRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public TratamentoRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<Tratamento?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Tratamentos
            .Include(t => t.Horarios)
            .FirstOrDefaultAsync(t => t.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tratamento>> DaFichaAsync(Guid fichaId, EstadoTratamento? estado, CancellationToken cancelamento)
    {
        var consulta = this.contexto.Tratamentos
            .AsNoTracking()
            .Include(t => t.Horarios)
            .Where(t => t.FichaId == fichaId);
        if (estado is not null)
        {
            consulta = consulta.Where(t => t.Estado == estado);
        }

        return await consulta
            .OrderByDescending(t => t.DataInicio)
            .ThenBy(t => t.Medicamento)
            .ThenBy(t => t.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Adicionar(Tratamento tratamento)
    {
        this.contexto.Tratamentos.Add(tratamento);
    }
}
