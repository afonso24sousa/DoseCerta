// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Emails;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Notificacoes;

/// <summary>
/// Cria as notificações de um evento para os Cuidadores ou os Familiares de uma ficha, uma por destinatário, e regista o e-mail de
/// cada uma na mesma transação; consulta o histórico de notificações de cada Utilizador (F011, RF-140 a RF-142). Não cria
/// notificações de fichas arquivadas (RF-122, RF-142).
/// </summary>
public sealed class NotificacaoService
{
    private readonly INotificacaoRepositorio notificacoes;
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly EnvioEmailService envioEmail;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificacaoService"/> class.
    /// </summary>
    /// <param name="notificacoes">Repositório das notificações.</param>
    /// <param name="utilizadores">Repositório dos utilizadores, para o e-mail de cada destinatário.</param>
    /// <param name="envioEmail">Registo do e-mail de cada notificação.</param>
    /// <param name="relogio">Relógio do sistema, para os dias de Lisboa do histórico.</param>
    public NotificacaoService(
        INotificacaoRepositorio notificacoes,
        IUtilizadorRepositorio utilizadores,
        EnvioEmailService envioEmail,
        IRelogio relogio)
    {
        this.notificacoes = notificacoes;
        this.utilizadores = utilizadores;
        this.envioEmail = envioEmail;
        this.relogio = relogio;
    }

    /// <summary>
    /// Notifica todos os Cuidadores da ficha, incluindo o Cuidador principal, na transação de quem a chama (RF-110 a RF-112).
    /// </summary>
    /// <param name="ficha">Ficha, com as associações.</param>
    /// <param name="tipo">Tipo da notificação.</param>
    /// <param name="referenciaTipo">Tipo do registo de origem.</param>
    /// <param name="referenciaId">Identificador do registo de origem.</param>
    /// <param name="instanteLimiteUtc">Instante-limite do evento, que o identifica (DM-20, RNF-043).</param>
    /// <param name="mensagem">Mensagem em português europeu.</param>
    /// <param name="agoraUtc">Instante da criação, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notificações criadas; vazia se a ficha estiver arquivada.</returns>
    public Task<IReadOnlyList<Notificacao>> NotificarCuidadoresAsync(
        Ficha ficha,
        TipoNotificacao tipo,
        TipoReferencia referenciaTipo,
        Guid referenciaId,
        DateTime instanteLimiteUtc,
        string mensagem,
        DateTime agoraUtc,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        var cuidadores = ficha.Associacoes.Where(a => a.EhCuidador()).Select(a => a.UtilizadorId);
        return this.NotificarAsync(ficha, cuidadores, new Origem(tipo, referenciaTipo, referenciaId, instanteLimiteUtc, mensagem), agoraUtc, cancelamento);
    }

    /// <summary>
    /// Notifica todos os Familiares da ficha, na transação de quem a chama; sem Familiares não cria nada (RF-113 CA3).
    /// </summary>
    /// <param name="ficha">Ficha, com as associações.</param>
    /// <param name="tipo">Tipo da notificação.</param>
    /// <param name="referenciaTipo">Tipo do registo de origem.</param>
    /// <param name="referenciaId">Identificador do registo de origem.</param>
    /// <param name="instanteLimiteUtc">Instante-limite do evento, que o identifica (DM-20, RNF-043).</param>
    /// <param name="mensagem">Mensagem em português europeu.</param>
    /// <param name="agoraUtc">Instante da criação, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notificações criadas; vazia se a ficha estiver arquivada ou não tiver Familiares.</returns>
    public Task<IReadOnlyList<Notificacao>> NotificarFamiliaresAsync(
        Ficha ficha,
        TipoNotificacao tipo,
        TipoReferencia referenciaTipo,
        Guid referenciaId,
        DateTime instanteLimiteUtc,
        string mensagem,
        DateTime agoraUtc,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        var familiares = ficha.Associacoes.Where(a => a.Papel == Papel.Familiar).Select(a => a.UtilizadorId);
        return this.NotificarAsync(ficha, familiares, new Origem(tipo, referenciaTipo, referenciaId, instanteLimiteUtc, mensagem), agoraUtc, cancelamento);
    }

    /// <summary>
    /// OP-51: lista as notificações recebidas pelo Utilizador, da mais recente para a mais antiga, e nunca as de outros
    /// Utilizadores, com o estado do e-mail (RF-119, RF-140).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="de">Primeiro dia, em hora de Lisboa; opcional.</param>
    /// <param name="ate">Último dia, inclusive, em hora de Lisboa; opcional e não anterior a <paramref name="de"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notificações do Utilizador.</returns>
    /// <exception cref="ValidacaoException">Se <paramref name="ate"/> for anterior a <paramref name="de"/>.</exception>
    public Task<IReadOnlyList<ItemNotificacao>> HistoricoAsync(Guid utilizadorId, DateOnly? de, DateOnly? ate, CancellationToken cancelamento)
    {
        if (de is not null && ate is not null && ate < de)
        {
            var validador = new ValidadorDados();
            validador.Erro("ate", "Não pode ser anterior à data de início (de).");
            validador.LancarSeInvalido();
        }

        var calendario = this.relogio.Calendario();
        DateTime? deUtc = de is DateOnly inicio ? calendario.InicioDoDiaUtc(inicio) : null;
        DateTime? ateUtc = ate is DateOnly fim ? calendario.InicioDoDiaSeguinteUtc(fim) : null;
        return this.notificacoes.DoUtilizadorAsync(utilizadorId, deUtc, ateUtc, cancelamento);
    }

    private async Task<IReadOnlyList<Notificacao>> NotificarAsync(
        Ficha ficha,
        IEnumerable<Guid> destinatarios,
        Origem origem,
        DateTime agoraUtc,
        CancellationToken cancelamento)
    {
        var criadas = new List<Notificacao>();
        if (!ficha.EstaAtiva())
        {
            return criadas;
        }

        foreach (var destinatarioId in destinatarios.Distinct().ToList())
        {
            var destinatario = await this.utilizadores.ObterPorIdAsync(destinatarioId, cancelamento).ConfigureAwait(false);
            if (destinatario is null)
            {
                continue;
            }

            var notificacao = new Notificacao(
                destinatarioId,
                ficha.Id,
                origem.Tipo,
                origem.Mensagem,
                origem.ReferenciaTipo,
                origem.ReferenciaId,
                origem.InstanteLimiteUtc,
                agoraUtc);
            this.notificacoes.Adicionar(notificacao);
            this.envioEmail.RegistarDeNotificacao(notificacao, destinatario.Email, agoraUtc);
            criadas.Add(notificacao);
        }

        return criadas;
    }

    private sealed record Origem(
        TipoNotificacao Tipo,
        TipoReferencia ReferenciaTipo,
        Guid ReferenciaId,
        DateTime InstanteLimiteUtc,
        string Mensagem);
}
