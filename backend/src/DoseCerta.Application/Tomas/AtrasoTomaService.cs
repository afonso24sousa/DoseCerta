// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Passos 2 e 3 do processo periódico: deteção das tomas por confirmar depois da tolerância do grau e notificação dos Cuidadores
/// e, no grau Grave, dos Familiares (F011, RF-110 a RF-113; TR-05 e TR-07 do diagrama de estados da toma). A toma continua
/// <c>Pendente</c>: só a marca do aviso muda (RF-066). Cada toma é tratada na sua transação, com a marca e as notificações juntas,
/// para nunca haver notificações duplicadas nem perdidas (RNF-042, RNF-043).
/// </summary>
public sealed class AtrasoTomaService
{
    private readonly ITomaRepositorio tomas;
    private readonly ITratamentoRepositorio tratamentos;
    private readonly IFichaRepositorio fichas;
    private readonly NotificacaoService notificacoes;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtrasoTomaService"/> class.
    /// </summary>
    /// <param name="tomas">Repositório das tomas.</param>
    /// <param name="tratamentos">Repositório dos tratamentos, para a ficha de cada toma.</param>
    /// <param name="fichas">Repositório das fichas, para o estado, o nome e as pessoas associadas.</param>
    /// <param name="notificacoes">Criação das notificações e dos e-mails.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada toma.</param>
    /// <param name="relogio">Relógio do sistema, para a hora prevista em hora de Lisboa.</param>
    public AtrasoTomaService(
        ITomaRepositorio tomas,
        ITratamentoRepositorio tratamentos,
        IFichaRepositorio fichas,
        NotificacaoService notificacoes,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.tomas = tomas;
        this.tratamentos = tratamentos;
        this.fichas = fichas;
        this.notificacoes = notificacoes;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// Passo 2 do processo periódico: para cada toma pendente de ficha ativa cujo fim de tolerância já passou (estritamente
    /// depois: Leve 2 h, Moderado 1 h, Grave 30 min), marca o aviso e notifica todos os Cuidadores, uma só vez (RF-110 a RF-112,
    /// TR-05). Uma toma confirmada antes da deteção não gera notificação.
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas cujos Cuidadores foram notificados.</returns>
    public async Task<int> NotificarCuidadoresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var porNotificar = await this.tomas.PorNotificarCuidadoresAsync(agoraUtc, cancelamento).ConfigureAwait(false);
        var notificadas = 0;
        foreach (var tomaId in porNotificar)
        {
            if (await this.NotificarAsync(tomaId, paraFamiliares: false, agoraUtc, cancelamento).ConfigureAwait(false))
            {
                notificadas++;
            }
        }

        return notificadas;
    }

    /// <summary>
    /// Passo 3 do processo periódico: para cada toma Grave pendente de ficha ativa sem confirmação 60 minutos depois da hora
    /// prevista (estritamente depois), isto é, 30 minutos depois do aviso aos Cuidadores, marca o aviso e notifica todos os
    /// Familiares, uma só vez; sem Familiares não cria nada (RF-113, TR-07). Nos graus Leve e Moderado nunca notifica Familiares.
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas avisadas aos Familiares.</returns>
    public async Task<int> NotificarFamiliaresAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        var porNotificar = await this.tomas.PorNotificarFamiliaresAsync(agoraUtc, cancelamento).ConfigureAwait(false);
        var notificadas = 0;
        foreach (var tomaId in porNotificar)
        {
            if (await this.NotificarAsync(tomaId, paraFamiliares: true, agoraUtc, cancelamento).ConfigureAwait(false))
            {
                notificadas++;
            }
        }

        return notificadas;
    }

    private static bool Devido(Toma toma, bool paraFamiliares, DateTime agoraUtc)
    {
        return paraFamiliares ? toma.AtrasoFamiliaresDevido(agoraUtc) : toma.AtrasoCuidadoresDevido(agoraUtc);
    }

    private async Task<bool> NotificarAsync(Guid tomaId, bool paraFamiliares, DateTime agoraUtc, CancellationToken cancelamento)
    {
        try
        {
            return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
                async () =>
                {
                    // A toma é lida de novo na transação: se entretanto foi confirmada, já não é devida (RF-110 CA2, RF-113 CA2).
                    var toma = await this.tomas.ObterAsync(tomaId, cancelamento).ConfigureAwait(false);
                    if (toma is null || !Devido(toma, paraFamiliares, agoraUtc))
                    {
                        return false;
                    }

                    var ficha = await this.FichaDaTomaAsync(toma, cancelamento).ConfigureAwait(false);
                    var instanteLimiteUtc = paraFamiliares ? toma.InstanteFamiliaresGrave() : toma.FimTolerancia();

                    // Ficha arquivada, ou arquivada no instante-limite e já reativada: não há notificação (RF-142 CA2, TR-10).
                    if (ficha is null || !ficha.EstaAtiva() || instanteLimiteUtc < ficha.AtivaDesdeUtc)
                    {
                        return false;
                    }

                    var horaPrevista = this.relogio.Calendario().ParaLocal(toma.PrevistaEmUtc).ToString("HH:mm", CultureInfo.InvariantCulture);
                    var mensagem = TextosNotificacao.TomaPorConfirmar(toma.Medicamento, ficha.NomeCompleto, horaPrevista);
                    if (paraFamiliares)
                    {
                        toma.MarcarFamiliaresNotificados(agoraUtc);
                        await this.notificacoes.NotificarFamiliaresAsync(
                            ficha, TipoNotificacao.TomaGraveFamiliares, TipoReferencia.Toma, toma.Id, instanteLimiteUtc, mensagem, agoraUtc, cancelamento)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        toma.MarcarCuidadoresNotificados(agoraUtc);
                        await this.notificacoes.NotificarCuidadoresAsync(
                            ficha, TipoNotificacao.TomaEmAtraso, TipoReferencia.Toma, toma.Id, instanteLimiteUtc, mensagem, agoraUtc, cancelamento)
                            .ConfigureAwait(false);
                    }

                    return true;
                },
                cancelamento).ConfigureAwait(false);
        }
        catch (ConflitoDeVersaoException)
        {
            // Um Cuidador confirmou a toma entre a leitura e a gravação: a transação foi desfeita e não há notificação (RNF-044).
            return false;
        }
    }

    private async Task<Ficha?> FichaDaTomaAsync(Toma toma, CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentos.ObterPorIdAsync(toma.TratamentoId, cancelamento).ConfigureAwait(false);
        return tratamento is null ? null : await this.fichas.ObterCompletaAsync(tratamento.FichaId, cancelamento).ConfigureAwait(false);
    }
}
