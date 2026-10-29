// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Emails;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core dos e-mails a enviar.
/// </summary>
public sealed class EnvioEmailRepositorio : IEnvioEmailRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public EnvioEmailRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public void Adicionar(EnvioEmail envio)
    {
        this.contexto.EnviosEmail.Add(envio);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EnvioEmail>> PendentesDevidosAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        return await this.contexto.EnviosEmail
            .Where(e => e.Estado == EstadoEnvioEmail.Pendente && e.ProximaTentativaEmUtc <= agoraUtc)
            .OrderBy(e => e.ProximaTentativaEmUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificacaoSemEmail>> NotificacoesSemEmailAsync(CancellationToken cancelamento)
    {
        return await (
            from n in this.contexto.Notificacoes.AsNoTracking()
            join u in this.contexto.Utilizadores on n.DestinatarioId equals u.Id
            where !this.contexto.EnviosEmail.Any(e => e.NotificacaoId == n.Id)
            orderby n.CriadaEmUtc, n.Id
            select new NotificacaoSemEmail(n, u.Email))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<Notificacao?> ObterNotificacaoAsync(Guid notificacaoId, CancellationToken cancelamento)
    {
        return this.contexto.Notificacoes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notificacaoId, cancelamento);
    }
}
