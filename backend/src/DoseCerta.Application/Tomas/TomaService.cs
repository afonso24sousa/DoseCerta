// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Tomas previstas: geração, regeneração e remoção ao registar, alterar e terminar um tratamento (F005, RF-060), e consulta,
/// confirmação, calendário dos alarmes e passos do processo periódico (F006, RF-061 a RF-069). As operações chamadas pelo
/// <c>TratamentoService</c> e pelo <c>FichaService</c> correm dentro da transação de quem as chama.
/// </summary>
public sealed class TomaService
{
    private const string MensagemNaoEncontrada = "A toma não existe ou pertence a uma ficha a que não está associado.";

    private readonly ITomaRepositorio tomas;
    private readonly IFichaRepositorio fichas;
    private readonly ITratamentoRepositorio tratamentos;
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomaService"/> class.
    /// </summary>
    /// <param name="tomas">Repositório das tomas.</param>
    /// <param name="fichas">Repositório das fichas.</param>
    /// <param name="tratamentos">Repositório dos tratamentos.</param>
    /// <param name="utilizadores">Repositório dos utilizadores, para o nome de quem confirmou.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema, com o calendário de Lisboa.</param>
    public TomaService(
        ITomaRepositorio tomas,
        IFichaRepositorio fichas,
        ITratamentoRepositorio tratamentos,
        IUtilizadorRepositorio utilizadores,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.tomas = tomas;
        this.fichas = fichas;
        this.tratamentos = tratamentos;
        this.utilizadores = utilizadores;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// Gera as tomas de um tratamento registado: uma por horário e por dia entre a data de início e a data de fim; sem data
    /// de fim, até ao fim da janela móvel de 30 dias. Não gera instantes anteriores a <see cref="Ficha.AtivaDesdeUtc"/> nem
    /// tomas de fichas arquivadas (RF-060, RF-069, DM-19).
    /// </summary>
    /// <param name="tratamento">Tratamento registado.</param>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas geradas.</returns>
    public async Task<int> GerarParaTratamentoAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);

        var calendario = this.relogio.Calendario();
        return await this.GerarAsync(tratamento, calendario.InicioDoDiaUtc(tratamento.DataInicio), agoraUtc, cancelamento)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Volta a gerar as tomas futuras de um tratamento alterado: apaga as pendentes previstas depois de agora e gera-as com os
    /// dados e os horários novos; as anteriores mantêm a hora e o estado (RF-055, RF-060).
    /// </summary>
    /// <param name="tratamento">Tratamento alterado.</param>
    /// <param name="agoraUtc">Instante da alteração, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task RegerarFuturasAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);

        await this.RemoverFuturasAsync(tratamento, agoraUtc, cancelamento).ConfigureAwait(false);
        await this.GerarAsync(tratamento, agoraUtc, agoraUtc, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove as tomas pendentes de um tratamento previstas depois de agora, ao terminá-lo; as anteriores mantêm a hora e o
    /// estado (RF-056, RF-060).
    /// </summary>
    /// <param name="tratamento">Tratamento terminado.</param>
    /// <param name="agoraUtc">Instante do término, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public Task RemoverFuturasAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);
        return this.tomas.EliminarFuturasPendentesAsync(tratamento.Id, agoraUtc, cancelamento);
    }

    /// <summary>
    /// OP-32: lista as tomas de uma ficha previstas entre dois dias de Lisboa, a Cuidadores e Familiares associados, também numa
    /// ficha arquivada (RF-063, RF-065).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="de">Primeiro dia, em hora de Lisboa; obrigatório.</param>
    /// <param name="ate">Último dia, em hora de Lisboa; obrigatório e no máximo 31 dias depois de <paramref name="de"/>, contando os dois.</param>
    /// <param name="estado">Filtro opcional pelo estado.</param>
    /// <param name="tratamentoId">Filtro opcional pelo tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas, por ordem do instante previsto.</returns>
    /// <exception cref="ValidacaoException">Se o intervalo faltar ou for inválido.</exception>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaNaoEncontrada"/>, a quem não está associado.</exception>
    public async Task<IReadOnlyList<TomaDaFicha>> ListarAsync(
        Guid utilizadorId,
        Guid fichaId,
        DateOnly? de,
        DateOnly? ate,
        EstadoToma? estado,
        Guid? tratamentoId,
        CancellationToken cancelamento)
    {
        ValidarIntervalo(de, ate);
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);

        var calendario = this.relogio.Calendario();
        return await this.tomas
            .DoPeriodoAsync(fichaId, calendario.InicioDoDiaUtc(de!.Value), calendario.InicioDoDiaSeguinteUtc(ate!.Value), estado, tratamentoId, cancelamento)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// OP-33: confirma uma toma, registando o Cuidador e o instante; até ao fim da tolerância fica <c>Confirmada</c> e depois
    /// <c>ConfirmadaComAtraso</c>. Qualquer Cuidador associado confirma, também numa ficha arquivada; a primeira confirmação
    /// mantém-se (RF-064 a RF-067, RF-105, RNF-044).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="tomaId">Toma a confirmar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A toma confirmada, com o nome de quem confirmou.</returns>
    /// <exception cref="DominioException">
    /// Por esta ordem: <see cref="CodigosErro.RecursoNaoEncontrado"/> (404), <see cref="CodigosErro.AcessoProibido"/> (403) e
    /// <see cref="CodigosErro.TomaJaConfirmada"/>, <see cref="CodigosErro.TomaNaoConfirmavel"/> ou
    /// <see cref="CodigosErro.TomaForaDaJanela"/> (409).
    /// </exception>
    public async Task<TomaDaFicha> ConfirmarAsync(Guid utilizadorId, Guid tomaId, CancellationToken cancelamento)
    {
        try
        {
            return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
                async () =>
                {
                    var toma = await this.ObterComAcessoAsync(utilizadorId, tomaId, cancelamento).ConfigureAwait(false);
                    if (toma.EstaConfirmada())
                    {
                        throw await this.JaConfirmadaAsync(toma, cancelamento).ConfigureAwait(false);
                    }

                    toma.Confirmar(utilizadorId, this.relogio.AgoraUtc(), this.relogio.Calendario());
                    var cuidador = await this.utilizadores.ObterPorIdAsync(utilizadorId, cancelamento).ConfigureAwait(false);
                    return new TomaDaFicha(toma, cuidador?.Nome);
                },
                cancelamento).ConfigureAwait(false);
        }
        catch (ConflitoDeVersaoException)
        {
            // Outro Cuidador confirmou ao mesmo tempo: devolve a confirmação que ficou gravada (RNF-044, TR-15).
            var gravada = await this.tomas.ObterAsync(tomaId, cancelamento).ConfigureAwait(false);
            if (gravada is not null && gravada.EstaConfirmada())
            {
                throw await this.JaConfirmadaAsync(gravada, cancelamento).ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>
    /// OP-34: obtém as tomas pendentes das próximas 48 horas das fichas ativas em que o Utilizador é Cuidador, para os alarmes
    /// locais do telemóvel (RF-061, RF-062, RF-069).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As tomas a sincronizar; vazia se não houver.</returns>
    public Task<IReadOnlyList<TomaParaSincronizar>> CalendarioParaSincronizarAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        return this.tomas.PendentesProximas48hAsync(utilizadorId, this.relogio.AgoraUtc(), cancelamento);
    }

    /// <summary>
    /// Passo 1 do processo periódico: gera as tomas em falta dos tratamentos em curso das fichas ativas até ao fim da janela
    /// móvel de 30 dias, numa transação própria (RF-060).
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas geradas.</returns>
    public async Task<int> PrologarJanelaMovelAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var emCurso = await this.tratamentos.EmCursoDeFichasAtivasAsync(cancelamento).ConfigureAwait(false);
                var geradas = 0;
                foreach (var tratamento in emCurso)
                {
                    geradas += await this.GerarAsync(tratamento, agoraUtc, agoraUtc, cancelamento).ConfigureAwait(false);
                }

                return geradas;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Passo 5 do processo periódico: passa a não confirmadas as tomas pendentes com 24 horas ou mais desde a hora prevista,
    /// numa transação própria; não cria notificação (RF-068, TR-06).
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas que passaram a não confirmadas.</returns>
    public async Task<int> ConcluirNaoConfirmadasAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var pendentes = await this.tomas.PendentesComVinteEQuatroHorasAsync(agoraUtc, cancelamento).ConfigureAwait(false);
                foreach (var toma in pendentes)
                {
                    toma.MarcarNaoConfirmada(agoraUtc);
                }

                return pendentes.Count;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Retoma a geração das tomas dos tratamentos em curso de uma ficha reativada, só a partir do instante da reativação:
    /// nenhuma toma para o período em que esteve arquivada (RF-069, RF-122, TR-10). Corre na transação de quem a chama.
    /// </summary>
    /// <param name="fichaId">Ficha reativada.</param>
    /// <param name="agoraUtc">Instante da reativação, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas geradas.</returns>
    public async Task<int> RetomarGeracaoDaFichaAsync(Guid fichaId, DateTime agoraUtc, CancellationToken cancelamento)
    {
        var emCurso = await this.tratamentos.DaFichaAsync(fichaId, EstadoTratamento.EmCurso, cancelamento).ConfigureAwait(false);
        var geradas = 0;
        foreach (var tratamento in emCurso)
        {
            geradas += await this.GerarAsync(tratamento, agoraUtc, agoraUtc, cancelamento).ConfigureAwait(false);
        }

        return geradas;
    }

    private static void ValidarIntervalo(DateOnly? de, DateOnly? ate)
    {
        var validador = new ValidadorDados();
        if (de is null)
        {
            validador.Erro("de", "É obrigatória, no formato AAAA-MM-DD.");
        }

        if (ate is null)
        {
            validador.Erro("ate", "É obrigatória, no formato AAAA-MM-DD.");
        }
        else if (ate < de)
        {
            validador.Erro("ate", "Não pode ser anterior à data de início (de).");
        }
        else if (de is not null && ate.Value.DayNumber - de.Value.DayNumber + 1 > ParametrosDeNegocio.IntervaloMaximoConsultaDias)
        {
            validador.Erro("ate", "O intervalo não pode ter mais de 31 dias.");
        }

        validador.LancarSeInvalido();
    }

    // Toma inexistente ou de ficha não associada: RECURSO_NAO_ENCONTRADO (RNF-005); Familiar: ACESSO_PROIBIDO (TR-12).
    // A ficha arquivada não é recusada: as tomas passadas ainda podem ser confirmadas.
    private async Task<Toma> ObterComAcessoAsync(Guid utilizadorId, Guid tomaId, CancellationToken cancelamento)
    {
        var toma = await this.tomas.ObterAsync(tomaId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrada);
        var tratamento = await this.tratamentos.ObterPorIdAsync(toma.TratamentoId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrada);
        try
        {
            await this.autorizacao.ExigirCuidadorAsync(utilizadorId, tratamento.FichaId, cancelamento).ConfigureAwait(false);
        }
        catch (DominioException erro) when (erro.Codigo == CodigosErro.FichaNaoEncontrada)
        {
            throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrada);
        }

        return toma;
    }

    private async Task<DominioException> JaConfirmadaAsync(Toma toma, CancellationToken cancelamento)
    {
        var porId = toma.ConfirmadaPorId!.Value;
        var emUtc = toma.ConfirmadaEmUtc!.Value;
        var cuidador = await this.utilizadores.ObterPorIdAsync(porId, cancelamento).ConfigureAwait(false);
        var nome = cuidador?.Nome ?? string.Empty;
        var local = this.relogio.Calendario().ParaLocal(emUtc);
        var mensagem = string.Format(
            CultureInfo.InvariantCulture,
            "A toma já foi confirmada por {0} em {1:dd/MM/yyyy} às {1:HH:mm}.",
            nome,
            local);
        return new DominioException(CodigosErro.TomaJaConfirmada, mensagem)
        {
            Confirmacao = new ConfirmacaoRegistada(porId, nome, emUtc),
        };
    }

    private async Task<int> GerarAsync(Tratamento tratamento, DateTime aPartirDeUtc, DateTime agoraUtc, CancellationToken cancelamento)
    {
        var ficha = await this.fichas.ObterCompletaAsync(tratamento.FichaId, cancelamento).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O tratamento pertence a uma ficha inexistente.");
        if (!ficha.EstaAtiva())
        {
            return 0;
        }

        var calendario = this.relogio.Calendario();
        var deUtc = aPartirDeUtc > ficha.AtivaDesdeUtc ? aPartirDeUtc : ficha.AtivaDesdeUtc;
        var ateUtc = tratamento.DataFim is DateOnly fim
            ? calendario.InicioDoDiaSeguinteUtc(fim)
            : agoraUtc.AddDays(ParametrosDeNegocio.JanelaMovelDias);

        var geradas = 0;
        foreach (var toma in tratamento.GerarTomas(deUtc, ateUtc, calendario))
        {
            if (await this.tomas.AdicionarSeNaoExisteAsync(toma, cancelamento).ConfigureAwait(false))
            {
                geradas++;
            }
        }

        return geradas;
    }
}
