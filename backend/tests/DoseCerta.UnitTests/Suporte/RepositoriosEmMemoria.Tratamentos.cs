// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositórios em memória dos tratamentos (F005) e das tomas (F005, F006).
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
    public Task<IReadOnlyList<Tratamento>> EmCursoDeFichasAtivasAsync(CancellationToken cancelamento)
    {
        IReadOnlyList<Tratamento> lista = this.Tratamentos
            .Where(t => t.Estado == EstadoTratamento.EmCurso && this.Fichas.Exists(f => f.Id == t.FichaId && f.EstaAtiva()))
            .ToList();
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

    /// <inheritdoc/>
    public Task<Toma?> ObterAsync(Guid tomaId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Tomas.FirstOrDefault(t => t.Id == tomaId));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TomaDaFicha>> DoPeriodoAsync(
        Guid fichaId,
        DateTime deUtc,
        DateTime ateUtc,
        EstadoToma? estado,
        Guid? tratamentoId,
        CancellationToken cancelamento)
    {
        IReadOnlyList<TomaDaFicha> lista = this.Tomas
            .Where(t => this.FichaDaToma(t)?.Id == fichaId
                && t.PrevistaEmUtc >= deUtc
                && t.PrevistaEmUtc < ateUtc
                && (estado is null || t.Estado == estado)
                && (tratamentoId is null || t.TratamentoId == tratamentoId))
            .OrderBy(t => t.PrevistaEmUtc)
            .Select(t => new TomaDaFicha(t, this.Utilizadores.FirstOrDefault(u => u.Id == t.ConfirmadaPorId)?.Nome))
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TomaParaSincronizar>> PendentesProximas48hAsync(Guid utilizadorId, DateTime agoraUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<TomaParaSincronizar> lista = this.Tomas
            .Where(t => t.Estado == EstadoToma.Pendente
                && t.PrevistaEmUtc >= agoraUtc
                && t.PrevistaEmUtc < agoraUtc + ParametrosDeNegocio.JanelaSincronizacao)
            .Select(t => (Toma: t, Ficha: this.FichaDaToma(t)))
            .Where(x => x.Ficha is not null && x.Ficha.EstaAtiva() && x.Ficha.ObterAssociacao(utilizadorId)?.EhCuidador() == true)
            .OrderBy(x => x.Toma.PrevistaEmUtc)
            .Select(x => new TomaParaSincronizar(x.Toma.Id, x.Ficha!.Id, x.Ficha.NomeCompleto, x.Toma.Medicamento, x.Toma.Dose, x.Toma.PrevistaEmUtc))
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Toma>> PendentesComVinteEQuatroHorasAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<Toma> lista = this.Tomas
            .Where(t => t.Estado == EstadoToma.Pendente && t.PrevistaEmUtc + PoliticaTolerancia.LimiteNaoConfirmada <= agoraUtc)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    Task<IReadOnlyList<TomaNaFicha>> ITomaRepositorio.DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<TomaNaFicha> lista = this.Tomas
            .Where(t => t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc)
            .Select(t => (Toma: t, Ficha: this.FichaDaToma(t)))
            .Where(x => x.Ficha is not null && fichaIds.Contains(x.Ficha.Id))
            .OrderBy(x => x.Toma.PrevistaEmUtc)
            .Select(x => new TomaNaFicha(x.Ficha!.Id, x.Toma))
            .ToList();
        return Task.FromResult(lista);
    }

    private Ficha? FichaDaToma(Toma toma)
    {
        var tratamento = this.Tratamentos.FirstOrDefault(t => t.Id == toma.TratamentoId);
        return tratamento is null ? null : this.Fichas.FirstOrDefault(f => f.Id == tratamento.FichaId);
    }
}
