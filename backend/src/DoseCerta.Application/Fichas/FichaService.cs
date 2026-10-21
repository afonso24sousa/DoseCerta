// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Convites;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Criação (de terceiro ou própria), consulta, edição, arquivo, reativação e eliminação de fichas (F002, F003).
/// Cada escrita corre numa só transação, com os documentos e as associações (RF-020 a RF-031, RF-120 a RF-123, RNF-046).
/// </summary>
public sealed class FichaService
{
    /// <summary>
    /// Comprimento máximo do nome completo do utente (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoNome = 150;

    /// <summary>
    /// Mensagem do erro <c>FICHA_PROPRIA_JA_EXISTE</c>, igual na verificação prévia e na violação do índice único (RF-031).
    /// </summary>
    public const string MensagemFichaPropriaJaExiste = "Já tem uma ficha própria; pode criar fichas de terceiros.";

    private const string RegraListaFechada = "Tem de conter apenas valores das listas de GET /api/v1/dominios/necessidades.";

    private readonly IFichaRepositorio fichas;
    private readonly IDominiosRepositorio dominios;
    private readonly ConviteService conviteService;
    private readonly IDocumentoRepositorio documentos;
    private readonly INotificacaoRepositorio notificacoes;
    private readonly DocumentoService documentoService;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichaService"/> class.
    /// </summary>
    /// <param name="fichas">Repositório das fichas.</param>
    /// <param name="dominios">Repositório das listas fechadas.</param>
    /// <param name="conviteService">Serviço dos convites, que regista o convite para Cuidador principal.</param>
    /// <param name="documentos">Repositório dos documentos.</param>
    /// <param name="notificacoes">Repositório das notificações.</param>
    /// <param name="documentoService">Serviço dos documentos.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    public FichaService(
        IFichaRepositorio fichas,
        IDominiosRepositorio dominios,
        ConviteService conviteService,
        IDocumentoRepositorio documentos,
        INotificacaoRepositorio notificacoes,
        DocumentoService documentoService,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.fichas = fichas;
        this.dominios = dominios;
        this.conviteService = conviteService;
        this.documentos = documentos;
        this.notificacoes = notificacoes;
        this.documentoService = documentoService;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-09: cria uma ficha de terceiro ou a ficha própria. O criador fica associado como Cuidador principal; numa ficha de terceiro
    /// com <c>eu=false</c> fica Cuidador principal provisório e o convite para Cuidador principal fica pendente (RF-020 a RF-025, RF-030, RF-031).
    /// O convite é registado pelo <see cref="ConviteService"/>, como os convites de OP-19 (F004).
    /// </summary>
    /// <param name="criadorId">Utilizador autenticado.</param>
    /// <param name="dados">Dados de identificação e necessidades.</param>
    /// <param name="propria">Indica se é a ficha própria do criador.</param>
    /// <param name="principal">Definição do Cuidador principal, obrigatória numa ficha de terceiro.</param>
    /// <param name="documentosSubmetidos">Documentos a carregar com a ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A ficha criada e a associação do criador.</returns>
    public async Task<AcessoFicha> CriarAsync(
        Guid criadorId,
        DadosFicha dados,
        bool propria,
        DefinicaoPrincipal? principal,
        IReadOnlyList<DocumentoSubmetido> documentosSubmetidos,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(documentosSubmetidos);

        var validador = new ValidadorDados();
        var validados = await this.ValidarDadosAsync(dados, validador, cancelamento).ConfigureAwait(false);
        string? emailConvidado = null;
        if (!propria)
        {
            if (principal?.Eu is null)
            {
                validador.Erro(
                    "cuidadorPrincipal",
                    "É obrigatório numa ficha de terceiro: indique eu=true ou eu=false com o e-mail da pessoa a convidar.");
            }
            else if (principal.Eu == false)
            {
                emailConvidado = ValidadorDados.Normalizar(principal.Email);
                validador.Email("cuidadorPrincipal.email", emailConvidado);
            }
        }

        validador.LancarSeInvalido();
        var ficheiros = await DocumentoService.ValidarTodosAsync(documentosSubmetidos, cancelamento).ConfigureAwait(false);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                if (propria && await this.fichas.ExistePropriaDeAsync(criadorId, cancelamento).ConfigureAwait(false))
                {
                    throw new DominioException(CodigosErro.FichaPropriaJaExiste, MensagemFichaPropriaJaExiste);
                }

                var agora = this.relogio.AgoraUtc();
                var ficha = new Ficha(
                    validados.NomeCompleto,
                    validados.DataNascimento,
                    validados.ContactoEmergencia,
                    propria ? criadorId : null,
                    criadorId,
                    agora);
                ficha.DefinirNecessidades(validados.GrauAutonomia, validados.CondicoesSaude, validados.Alergias);
                var associacao = ficha.AssociarCuidadorPrincipal(criadorId, provisorio: emailConvidado is not null, agora);
                this.fichas.Adicionar(ficha);

                if (emailConvidado is not null)
                {
                    await this.conviteService.ConvidarParaPrincipalAsync(ficha, criadorId, emailConvidado, agora, cancelamento).ConfigureAwait(false);
                }

                foreach (var ficheiro in ficheiros)
                {
                    await this.documentoService.GuardarNaTransacaoAsync(ficha.Id, ficheiro, criadorId, agora, cancelamento).ConfigureAwait(false);
                }

                return new AcessoFicha(ficha, associacao);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-07: lista as fichas a que o Utilizador está associado, ativas, arquivadas ou ambas (RF-028, RNF-005).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="estado">Estado pedido, ou <see langword="null"/> para ativas e arquivadas.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As fichas, com o papel do Utilizador em cada uma.</returns>
    public async Task<IReadOnlyList<AcessoFicha>> ListarAsync(Guid utilizadorId, EstadoFicha? estado, CancellationToken cancelamento)
    {
        var lista = await this.fichas.ListarDoUtilizadorAsync(utilizadorId, estado, cancelamento).ConfigureAwait(false);
        return lista
            .Select(f => (Ficha: f, Associacao: f.ObterAssociacao(utilizadorId)))
            .Where(par => par.Associacao is not null)
            .Select(par => new AcessoFicha(par.Ficha, par.Associacao!))
            .ToList();
    }

    /// <summary>
    /// OP-10: consulta os dados de identificação e as necessidades de uma ficha (RF-028).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A ficha e a associação do Utilizador.</returns>
    public Task<AcessoFicha> ObterAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        return this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento);
    }

    /// <summary>
    /// OP-11: edita os dados e as necessidades de uma ficha ativa e acrescenta documentos; só um Cuidador o pode fazer.
    /// Se um documento não puder ser guardado, a ficha fica exatamente como estava (RF-021 a RF-023, RF-029, RNF-046).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a editar.</param>
    /// <param name="dados">Dados de identificação e necessidades.</param>
    /// <param name="documentosSubmetidos">Documentos a acrescentar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A ficha atualizada e a associação do Utilizador.</returns>
    public async Task<AcessoFicha> EditarAsync(
        Guid utilizadorId,
        Guid fichaId,
        DadosFicha dados,
        IReadOnlyList<DocumentoSubmetido> documentosSubmetidos,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(documentosSubmetidos);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validador = new ValidadorDados();
                var validados = await this.ValidarDadosAsync(dados, validador, cancelamento).ConfigureAwait(false);
                validador.LancarSeInvalido();
                var ficheiros = await DocumentoService.ValidarTodosAsync(documentosSubmetidos, cancelamento).ConfigureAwait(false);

                // Os ficheiros são guardados primeiro: se algum falhar, nenhum dado da ficha é alterado (UC-08, passo 3).
                var agora = this.relogio.AgoraUtc();
                foreach (var ficheiro in ficheiros)
                {
                    await this.documentoService.GuardarNaTransacaoAsync(fichaId, ficheiro, utilizadorId, agora, cancelamento).ConfigureAwait(false);
                }

                acesso.Ficha.AtualizarDados(validados.NomeCompleto, validados.DataNascimento, validados.ContactoEmergencia);
                acesso.Ficha.DefinirNecessidades(validados.GrauAutonomia, validados.CondicoesSaude, validados.Alergias);
                return acesso;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-12: arquiva a ficha, preservando o histórico, e apaga as tomas e as execuções futuras ainda por confirmar (RF-120, RF-122).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a arquivar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task ArquivarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var agora = this.relogio.AgoraUtc();
                acesso.Ficha.Arquivar(agora);
                await this.fichas.EliminarFuturasPorConfirmarAsync(fichaId, agora, cancelamento).ConfigureAwait(false);
                return true;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-13: reativa uma ficha arquivada; a geração de eventos retoma a partir deste instante (RF-121, RF-122).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a reativar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task ReativarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                acesso.Ficha.Reativar(this.relogio.AgoraUtc());
                return true;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-14: elimina uma ficha sem medicação, tarefas nem histórico. As notificações são apagadas na mesma transação e os ficheiros
    /// dos documentos são removidos depois de a transação ser confirmada (RF-123, RNF-046).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a eliminar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task EliminarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        var caminhos = await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var temMedicacao = await this.fichas.TemMedicacaoAsync(fichaId, cancelamento).ConfigureAwait(false);
                var temTarefas = await this.fichas.TemTarefasAsync(fichaId, cancelamento).ConfigureAwait(false);
                var temHistorico = await this.fichas.TemHistoricoAsync(fichaId, cancelamento).ConfigureAwait(false);
                if (!Ficha.PodeSerEliminada(temMedicacao, temTarefas, temHistorico))
                {
                    throw new DominioException(
                        CodigosErro.FichaComHistorico,
                        "A ficha tem medicação, tarefas ou histórico registados e não pode ser eliminada; só pode ser arquivada.");
                }

                var ficheiros = await this.documentos.DaFichaAsync(fichaId, cancelamento).ConfigureAwait(false);
                await this.notificacoes.EliminarDaFichaAsync(fichaId, cancelamento).ConfigureAwait(false);
                this.fichas.Remover(acesso.Ficha);
                return ficheiros.Select(d => d.CaminhoRelativo).ToList();
            },
            cancelamento).ConfigureAwait(false);

        this.documentoService.EliminarFicheiros(caminhos);
    }

    /// <summary>
    /// OP-08: obtém as listas fechadas de condições de saúde, alergias e graus de autonomia (RF-021).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As três listas.</returns>
    public async Task<DominiosNecessidades> ObterDominiosAsync(CancellationToken cancelamento)
    {
        var condicoes = await this.dominios.ListarCondicoesAsync(cancelamento).ConfigureAwait(false);
        var alergias = await this.dominios.ListarAlergiasAsync(cancelamento).ConfigureAwait(false);
        var graus = await this.dominios.ListarGrausAutonomiaAsync(cancelamento).ConfigureAwait(false);
        return new DominiosNecessidades(condicoes, alergias, graus);
    }

    private async Task<DadosValidados> ValidarDadosAsync(DadosFicha dados, ValidadorDados validador, CancellationToken cancelamento)
    {
        var nome = ValidadorDados.Normalizar(dados.NomeCompleto);
        var contacto = ValidadorDados.Normalizar(dados.ContactoEmergencia);
        validador.Obrigatorio("nomeCompleto", nome, ComprimentoMaximoNome);

        var hoje = this.relogio.Calendario().DiaLocal(this.relogio.AgoraUtc());
        if (dados.DataNascimento is null)
        {
            validador.Erro("dataNascimento", "É obrigatória, no formato AAAA-MM-DD.");
        }
        else if (dados.DataNascimento > hoje)
        {
            validador.Erro("dataNascimento", "Não pode ser uma data futura.");
        }
        else if (dados.DataNascimento < ParametrosDeNegocio.DataNascimentoMinima)
        {
            validador.Erro("dataNascimento", "Tem de ser igual ou posterior a 1900-01-01.");
        }

        validador.Telefone("contactoEmergencia", contacto);

        GrauAutonomia? grau = null;
        if (dados.GrauAutonomiaId is short grauId)
        {
            grau = await this.dominios.GrauPorIdAsync(grauId, cancelamento).ConfigureAwait(false);
            if (grau is null)
            {
                validador.Erro("grauAutonomiaId", "Tem de ser um valor da lista de graus de autonomia de GET /api/v1/dominios/necessidades.");
            }
        }

        var idsCondicoes = (dados.CondicoesSaudeIds ?? Array.Empty<short>()).Distinct().ToList();
        var condicoes = await this.dominios.CondicoesPorIdsAsync(idsCondicoes, cancelamento).ConfigureAwait(false);
        if (condicoes.Count != idsCondicoes.Count)
        {
            validador.Erro("condicoesSaudeIds", RegraListaFechada);
        }

        var idsAlergias = (dados.AlergiasIds ?? Array.Empty<short>()).Distinct().ToList();
        var alergias = await this.dominios.AlergiasPorIdsAsync(idsAlergias, cancelamento).ConfigureAwait(false);
        if (alergias.Count != idsAlergias.Count)
        {
            validador.Erro("alergiasIds", RegraListaFechada);
        }

        return new DadosValidados(nome ?? string.Empty, dados.DataNascimento ?? default, contacto ?? string.Empty, grau, condicoes, alergias);
    }

    private sealed record DadosValidados(
        string NomeCompleto,
        DateOnly DataNascimento,
        string ContactoEmergencia,
        GrauAutonomia? GrauAutonomia,
        IReadOnlyList<CondicaoSaude> CondicoesSaude,
        IReadOnlyList<Alergia> Alergias);
}
