// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Compromissos;

/// <summary>
/// Passo 6 do processo periódico: aviso antes de um compromisso (F011, RF-116, RF-117). Quando chega o instante do aviso (a
/// data e hora menos a antecedência), notifica uma vez cada Cuidador (incluindo o principal) e cada Familiar da ficha, na
/// aplicação e por e-mail. Um compromisso registado depois desse instante é avisado no ciclo seguinte (RF-117 CA3); um
/// compromisso cuja data já passou nunca é avisado. Cada compromisso é tratado na sua transação, com a marca do aviso e as
/// notificações juntas, para nunca haver avisos duplicados nem perdidos (RNF-042, RNF-043).
/// </summary>
public sealed class AvisoCompromissoService
{
    private readonly ICompromissoRepositorio compromissos;
    private readonly IFichaRepositorio fichas;
    private readonly NotificacaoService notificacoes;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvisoCompromissoService"/> class.
    /// </summary>
    /// <param name="compromissos">Repositório dos compromissos.</param>
    /// <param name="fichas">Repositório das fichas, para o estado, o nome e as pessoas associadas.</param>
    /// <param name="notificacoes">Criação das notificações e dos e-mails.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada compromisso.</param>
    /// <param name="relogio">Relógio do sistema, para a data e a hora em hora de Lisboa.</param>
    public AvisoCompromissoService(
        ICompromissoRepositorio compromissos,
        IFichaRepositorio fichas,
        NotificacaoService notificacoes,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.compromissos = compromissos;
        this.fichas = fichas;
        this.notificacoes = notificacoes;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// Passo 6 do processo periódico: para cada compromisso de ficha ativa com o aviso devido e por enviar, marca o aviso e
    /// notifica todas as pessoas associadas à ficha, uma só vez (RF-117). Os compromissos removidos não são avisados (RF-117
    /// CA2) e os editados são avisados no instante novo (RF-081).
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de compromissos avisados.</returns>
    public async Task<int> AvisarProximosAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var porAvisar = await this.compromissos.PorAvisarAsync(agoraUtc, cancelamento).ConfigureAwait(false);
        var avisados = 0;
        foreach (var compromissoId in porAvisar)
        {
            if (await this.AvisarAsync(compromissoId, agoraUtc, cancelamento).ConfigureAwait(false))
            {
                avisados++;
            }
        }

        return avisados;
    }

    private Task<bool> AvisarAsync(Guid compromissoId, DateTime agoraUtc, CancellationToken cancelamento)
    {
        return this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                // O compromisso é lido de novo na transação: se entretanto foi removido ou avisado, já não é devido (RF-117 CA2).
                var compromisso = await this.compromissos.ObterPorIdAsync(compromissoId, cancelamento).ConfigureAwait(false);
                if (compromisso is null || !compromisso.AvisoDevido(agoraUtc))
                {
                    return false;
                }

                // Ficha arquivada, ou aviso que ficou devido enquanto esteve arquivada: não há notificação (RF-122, RF-142).
                var ficha = await this.fichas.ObterCompletaAsync(compromisso.FichaId, cancelamento).ConfigureAwait(false);
                if (ficha is null || !ficha.EstaAtiva() || compromisso.InstanteAvisoDevido() < ficha.AtivaDesdeUtc)
                {
                    return false;
                }

                // Marca condicional: outra execução que já o marcou, ou uma edição gravada depois da leitura, faz o aviso
                // esperar pelo ciclo seguinte, sem notificações duplicadas nem com os valores antigos (RNF-043, RNF-044).
                if (!await this.compromissos.MarcarAvisoEnviadoAsync(compromisso, agoraUtc, cancelamento).ConfigureAwait(false))
                {
                    return false;
                }

                var local = this.relogio.Calendario().ParaLocal(compromisso.DataHoraUtc);
                var mensagem = TextosNotificacao.ProximoCompromisso(
                    compromisso.Motivo,
                    compromisso.Local,
                    ficha.NomeCompleto,
                    local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    local.ToString("HH:mm", CultureInfo.InvariantCulture));
                await this.notificacoes.NotificarTodosAsync(
                    ficha,
                    TipoNotificacao.ProximoCompromisso,
                    TipoReferencia.Compromisso,
                    compromisso.Id,
                    compromisso.InstanteAviso(),
                    mensagem,
                    agoraUtc,
                    cancelamento).ConfigureAwait(false);
                return true;
            },
            cancelamento);
    }
}
