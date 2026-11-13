// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
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
    public async Task<IReadOnlyList<Guid>> PorAvisarAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        // O intervalo da data (agora, agora + 48 h] usa o índice filtrado IX_Compromisso_AvisoPendente; os compromissos já
        // passados ficam fora do intervalo sem precisarem de marca (RF-117, modelo de dados, secção 6.15). A última condição é
        // Compromisso.InstanteAvisoDevido() >= AtivaDesdeUtc, escrita como max(a, b) >= x <=> a >= x || b >= x (RF-142).
        var dataAteUtc = agoraUtc.AddMinutes(ParametrosDeNegocio.AntecedenciaMaximaMin);
        return await (
            from c in this.contexto.Compromissos.AsNoTracking()
            join ficha in this.contexto.Fichas on c.FichaId equals ficha.Id
            where c.AvisoEnviadoEmUtc == null
                && ficha.Estado == EstadoFicha.Ativa
                && c.DataHoraUtc > agoraUtc
                && c.DataHoraUtc <= dataAteUtc
                && c.DataHoraUtc.AddMinutes(-c.AntecedenciaAvisoMin) <= agoraUtc
                && (c.DataHoraUtc.AddMinutes(-c.AntecedenciaAvisoMin) >= ficha.AtivaDesdeUtc || c.CriadoEmUtc >= ficha.AtivaDesdeUtc)
            orderby c.DataHoraUtc, c.Id
            select c.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> MarcarAvisoEnviadoAsync(Compromisso lido, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(lido);

        // UPDATE condicional na transação em curso: uma execução concorrente espera pelo bloqueio da linha e, depois do
        // commit desta, já não a encontra por enviar (0 linhas), sem violar UQ_Notificacao_Origem (RNF-043).
        var alteradas = await this.contexto.Compromissos
            .Where(c => c.Id == lido.Id
                && c.AvisoEnviadoEmUtc == null
                && c.DataHoraUtc == lido.DataHoraUtc
                && c.AntecedenciaAvisoMin == lido.AntecedenciaAvisoMin)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.AvisoEnviadoEmUtc, agoraUtc), cancelamento)
            .ConfigureAwait(false);
        return alteradas == 1;
    }

    /// <inheritdoc/>
    public void GravarAvisoReposto(Compromisso compromisso)
    {
        ArgumentNullException.ThrowIfNull(compromisso);
        this.contexto.Entry(compromisso).Property(c => c.AvisoEnviadoEmUtc).IsModified = true;
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
