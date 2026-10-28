// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemNotificacao>> DoUtilizadorAsync(
        Guid utilizadorId,
        DateTime? deUtc,
        DateTime? ateUtc,
        CancellationToken cancelamento)
    {
        var notificacoes = this.contexto.Notificacoes.AsNoTracking().Where(n => n.DestinatarioId == utilizadorId);
        if (deUtc is not null)
        {
            notificacoes = notificacoes.Where(n => n.CriadaEmUtc >= deUtc);
        }

        if (ateUtc is not null)
        {
            notificacoes = notificacoes.Where(n => n.CriadaEmUtc < ateUtc);
        }

        var lidas = await (
            from n in notificacoes
            join f in this.contexto.Fichas on n.FichaId equals f.Id
            join t in this.contexto.Tomas on n.ReferenciaId equals t.Id into daToma
            from t in daToma.DefaultIfEmpty()
            join e in this.contexto.EnviosEmail on (Guid?)n.Id equals e.NotificacaoId into doEmail
            from e in doEmail.DefaultIfEmpty()
            orderby n.CriadaEmUtc descending, n.Id descending
            select new
            {
                Notificacao = n,
                NomeFicha = f.NomeCompleto,
                Medicamento = t == null ? null : t.Medicamento,
                PrevistaEmUtc = t == null ? (DateTime?)null : t.PrevistaEmUtc,
                Email = e == null ? (EstadoEnvioEmail?)null : e.Estado,
            })
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);

        // Sem e-mail registado, o envio ainda está por fazer: o processo periódico regista-o no ciclo seguinte (RF-141).
        return lidas
            .Select(l =>
            {
                var deToma = l.Notificacao.ReferenciaTipo == TipoReferencia.Toma;
                return new ItemNotificacao(
                    l.Notificacao.Id,
                    l.Notificacao.Tipo,
                    l.Notificacao.Mensagem,
                    l.Notificacao.FichaId,
                    l.NomeFicha,
                    deToma ? l.Medicamento : null,
                    deToma ? l.PrevistaEmUtc : null,
                    l.Notificacao.CriadaEmUtc,
                    l.Email ?? EstadoEnvioEmail.Pendente);
            })
            .ToList();
    }
}
