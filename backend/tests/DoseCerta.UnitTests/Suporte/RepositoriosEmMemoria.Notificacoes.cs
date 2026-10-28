// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Emails;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Consultas em memória das notificações de atraso das tomas, do histórico de notificações e dos e-mails das notificações (F011),
/// com as mesmas regras das consultas EF Core.
/// </summary>
public partial class RepositoriosEmMemoria
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<Guid>> PorNotificarCuidadoresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<Guid> lista = this.Tomas
            .Select(t => (Toma: t, Ficha: this.FichaDaToma(t)))
            .Where(x => x.Ficha is not null
                && x.Ficha.EstaAtiva()
                && x.Toma.AtrasoCuidadoresDevido(agoraUtc)
                && x.Toma.FimTolerancia() >= x.Ficha.AtivaDesdeUtc)
            .OrderBy(x => x.Toma.PrevistaEmUtc)
            .Select(x => x.Toma.Id)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Guid>> PorNotificarFamiliaresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<Guid> lista = this.Tomas
            .Select(t => (Toma: t, Ficha: this.FichaDaToma(t)))
            .Where(x => x.Ficha is not null
                && x.Ficha.EstaAtiva()
                && x.Toma.AtrasoFamiliaresDevido(agoraUtc)
                && x.Toma.InstanteFamiliaresGrave() >= x.Ficha.AtivaDesdeUtc)
            .OrderBy(x => x.Toma.PrevistaEmUtc)
            .Select(x => x.Toma.Id)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ItemNotificacao>> DoUtilizadorAsync(Guid utilizadorId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento)
    {
        IReadOnlyList<ItemNotificacao> lista = this.Notificacoes
            .Where(n => n.DestinatarioId == utilizadorId
                && (deUtc is null || n.CriadaEmUtc >= deUtc)
                && (ateUtc is null || n.CriadaEmUtc < ateUtc))
            .OrderByDescending(n => n.CriadaEmUtc)
            .Select(n =>
            {
                var toma = n.ReferenciaTipo == TipoReferencia.Toma ? this.Tomas.FirstOrDefault(t => t.Id == n.ReferenciaId) : null;
                var envio = this.EnviosEmail.FirstOrDefault(e => e.NotificacaoId == n.Id);
                return new ItemNotificacao(
                    n.Id,
                    n.Tipo,
                    n.Mensagem,
                    n.FichaId,
                    this.Fichas.First(f => f.Id == n.FichaId).NomeCompleto,
                    toma?.Medicamento,
                    toma?.PrevistaEmUtc,
                    n.CriadaEmUtc,
                    envio?.Estado ?? EstadoEnvioEmail.Pendente);
            })
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificacaoSemEmail>> NotificacoesSemEmailAsync(CancellationToken cancelamento)
    {
        IReadOnlyList<NotificacaoSemEmail> lista = this.Notificacoes
            .Where(n => !this.EnviosEmail.Exists(e => e.NotificacaoId == n.Id))
            .OrderBy(n => n.CriadaEmUtc)
            .Select(n => new NotificacaoSemEmail(n, this.Utilizadores.First(u => u.Id == n.DestinatarioId).Email))
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<Notificacao?> ObterNotificacaoAsync(Guid notificacaoId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Notificacoes.FirstOrDefault(n => n.Id == notificacaoId));
    }
}
