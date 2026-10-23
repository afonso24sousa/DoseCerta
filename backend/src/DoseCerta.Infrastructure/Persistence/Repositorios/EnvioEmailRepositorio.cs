// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

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
            .Where(e => e.Estado == EstadoEnvioEmail.Pendente && e.ProximaTentativaEmUtc <= agoraUtc && e.ConviteId != null)
            .OrderBy(e => e.ProximaTentativaEmUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }
}
