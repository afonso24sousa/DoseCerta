// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das tomas: geração e remoção (F005), consulta, calendário e passagem a não confirmada (F006), e
/// consulta da lista diária (F010).
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
    public Task<Toma?> ObterAsync(Guid tomaId, CancellationToken cancelamento)
    {
        return this.contexto.Tomas.FirstOrDefaultAsync(t => t.Id == tomaId, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TomaDaFicha>> DoPeriodoAsync(
        Guid fichaId,
        DateTime deUtc,
        DateTime ateUtc,
        EstadoToma? estado,
        Guid? tratamentoId,
        CancellationToken cancelamento)
    {
        var tomas = this.contexto.Tomas
            .AsNoTracking()
            .Where(t => t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc);
        if (estado is not null)
        {
            tomas = tomas.Where(t => t.Estado == estado);
        }

        if (tratamentoId is not null)
        {
            tomas = tomas.Where(t => t.TratamentoId == tratamentoId);
        }

        var lidas = await (
            from t in tomas
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            where tratamento.FichaId == fichaId
            join u in this.contexto.Utilizadores on t.ConfirmadaPorId equals (Guid?)u.Id into confirmou
            from u in confirmou.DefaultIfEmpty()
            orderby t.PrevistaEmUtc, t.Medicamento, t.Id
            select new { Toma = t, Nome = u == null ? null : u.Nome })
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
        return lidas.Select(l => new TomaDaFicha(l.Toma, l.Nome)).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TomaParaSincronizar>> PendentesProximas48hAsync(
        Guid utilizadorId,
        DateTime agoraUtc,
        CancellationToken cancelamento)
    {
        var limiteUtc = agoraUtc + ParametrosDeNegocio.JanelaSincronizacao;
        return await (
            from t in this.contexto.Tomas.AsNoTracking()
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            join ficha in this.contexto.Fichas on tratamento.FichaId equals ficha.Id
            where t.Estado == EstadoToma.Pendente
                && t.PrevistaEmUtc >= agoraUtc
                && t.PrevistaEmUtc < limiteUtc
                && ficha.Estado == EstadoFicha.Ativa
                && this.contexto.Associacoes.Any(a =>
                    a.FichaId == ficha.Id && a.UtilizadorId == utilizadorId && a.Papel == Papel.Cuidador)
            orderby t.PrevistaEmUtc, ficha.NomeCompleto, t.Id
            select new TomaParaSincronizar(t.Id, ficha.Id, ficha.NomeCompleto, t.Medicamento, t.Dose, t.PrevistaEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Toma>> PendentesComVinteEQuatroHorasAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var previstaAteUtc = agoraUtc - PoliticaTolerancia.LimiteNaoConfirmada;
        return await this.contexto.Tomas
            .Where(t => t.Estado == EstadoToma.Pendente && t.PrevistaEmUtc <= previstaAteUtc)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TomaNaFicha>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        var lidas = await (
            from t in this.contexto.Tomas.AsNoTracking()
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            where fichaIds.Contains(tratamento.FichaId) && t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc
            orderby t.PrevistaEmUtc, t.Id
            select new { tratamento.FichaId, Toma = t })
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
        return lidas.Select(l => new TomaNaFicha(l.FichaId, l.Toma)).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TomaDoDia>> DoDiaDosCuidadoresAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento)
    {
        return await (
            from t in this.contexto.Tomas.AsNoTracking()
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            join ficha in this.contexto.Fichas on tratamento.FichaId equals ficha.Id
            where fichaIds.Contains(ficha.Id) && t.PrevistaEmUtc >= deUtc && t.PrevistaEmUtc < ateUtc
            join u in this.contexto.Utilizadores on t.ConfirmadaPorId equals (Guid?)u.Id into confirmou
            from u in confirmou.DefaultIfEmpty()
            select new TomaDoDia(
                t.Id,
                ficha.Id,
                ficha.NomeCompleto,
                t.Medicamento,
                t.Dose,
                t.PrevistaEmUtc,
                t.Estado,
                u == null ? null : u.Nome,
                t.ConfirmadaEmUtc))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> PorNotificarCuidadoresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        // Fim da tolerância estritamente anterior a agora (prevista < agora - tolerância) e não anterior a AtivaDesdeUtc
        // (prevista >= AtivaDesdeUtc - tolerância), para cada grau (RF-110 a RF-112, RF-142).
        var leveAteUtc = agoraUtc - PoliticaTolerancia.Leve;
        var moderadoAteUtc = agoraUtc - PoliticaTolerancia.Moderado;
        var graveAteUtc = agoraUtc - PoliticaTolerancia.Grave;
        var leveMinutos = PoliticaTolerancia.Leve.TotalMinutes;
        var moderadoMinutos = PoliticaTolerancia.Moderado.TotalMinutes;
        var graveMinutos = PoliticaTolerancia.Grave.TotalMinutes;
        return await (
            from t in this.contexto.Tomas.AsNoTracking()
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            join ficha in this.contexto.Fichas on tratamento.FichaId equals ficha.Id
            where t.Estado == EstadoToma.Pendente
                && t.CuidadoresNotificadosEmUtc == null
                && ficha.Estado == EstadoFicha.Ativa
                && ((t.GrauImportancia == GrauImportancia.Leve
                        && t.PrevistaEmUtc < leveAteUtc
                        && t.PrevistaEmUtc >= ficha.AtivaDesdeUtc.AddMinutes(-leveMinutos))
                    || (t.GrauImportancia == GrauImportancia.Moderado
                        && t.PrevistaEmUtc < moderadoAteUtc
                        && t.PrevistaEmUtc >= ficha.AtivaDesdeUtc.AddMinutes(-moderadoMinutos))
                    || (t.GrauImportancia == GrauImportancia.Grave
                        && t.PrevistaEmUtc < graveAteUtc
                        && t.PrevistaEmUtc >= ficha.AtivaDesdeUtc.AddMinutes(-graveMinutos)))
            orderby t.PrevistaEmUtc, t.Id
            select t.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> PorNotificarFamiliaresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var previstaAteUtc = agoraUtc - PoliticaTolerancia.FamiliaresGrave;
        var familiaresMinutos = PoliticaTolerancia.FamiliaresGrave.TotalMinutes;
        return await (
            from t in this.contexto.Tomas.AsNoTracking()
            join tratamento in this.contexto.Tratamentos on t.TratamentoId equals tratamento.Id
            join ficha in this.contexto.Fichas on tratamento.FichaId equals ficha.Id
            where t.Estado == EstadoToma.Pendente
                && t.GrauImportancia == GrauImportancia.Grave
                && t.FamiliaresNotificadosEmUtc == null
                && ficha.Estado == EstadoFicha.Ativa
                && t.PrevistaEmUtc < previstaAteUtc
                && t.PrevistaEmUtc >= ficha.AtivaDesdeUtc.AddMinutes(-familiaresMinutos)
            orderby t.PrevistaEmUtc, t.Id
            select t.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }
}
