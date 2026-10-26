// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositórios em memória dos tratamentos e das tomas (F005).
/// </summary>
public partial class RepositoriosEmMemoria : ITratamentoRepositorio, ITomaRepositorio
{
    /// <summary>
    /// Gets os tratamentos registados.
    /// </summary>
    public List<Tratamento> Tratamentos { get; } = new();

    /// <summary>
    /// Gets as tomas registadas.
    /// </summary>
    public List<Toma> Tomas { get; } = new();

    /// <inheritdoc/>
    Task<Tratamento?> ITratamentoRepositorio.ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Tratamentos.FirstOrDefault(t => t.Id == id));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Tratamento>> DaFichaAsync(Guid fichaId, EstadoTratamento? estado, CancellationToken cancelamento)
    {
        IReadOnlyList<Tratamento> lista = this.Tratamentos
            .Where(t => t.FichaId == fichaId && (estado is null || t.Estado == estado))
            .OrderByDescending(t => t.DataInicio)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Tratamento>> ComDataFimUltrapassadaAsync(DateOnly hojeLocal, CancellationToken cancelamento)
    {
        IReadOnlyList<Tratamento> lista = this.Tratamentos.Where(t => t.DataFimUltrapassada(hojeLocal)).ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public void Adicionar(Tratamento tratamento)
    {
        this.Tratamentos.Add(tratamento);
    }

    /// <inheritdoc/>
    public Task<bool> AdicionarSeNaoExisteAsync(Toma toma, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(toma);
        if (this.Tomas.Exists(t => t.TratamentoId == toma.TratamentoId && t.PrevistaEmUtc == toma.PrevistaEmUtc))
        {
            return Task.FromResult(false);
        }

        this.Tomas.Add(toma);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<int> EliminarFuturasPendentesAsync(Guid tratamentoId, DateTime aPartirDeUtc, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Tomas.RemoveAll(t =>
            t.TratamentoId == tratamentoId && t.Estado == EstadoToma.Pendente && t.PrevistaEmUtc > aPartirDeUtc));
    }
}
