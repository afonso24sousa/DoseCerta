// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das notificações.
/// </summary>
public sealed class NotificacaoRepositorio : INotificacaoRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificacaoRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public NotificacaoRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<int> EliminarDaFichaAsync(Guid fichaId, CancellationToken cancelamento)
    {
        // Os EnvioEmail das notificações saem por cascata (FK_EnvioEmail_NotificacaoId).
        return this.contexto.Notificacoes.Where(n => n.FichaId == fichaId).ExecuteDeleteAsync(cancelamento);
    }

    /// <inheritdoc/>
    public void Adicionar(Notificacao notificacao)
    {
        this.contexto.Notificacoes.Add(notificacao);
    }
}
