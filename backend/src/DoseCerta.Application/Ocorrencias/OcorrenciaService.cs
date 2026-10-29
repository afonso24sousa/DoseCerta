// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Ocorrencias;

/// <summary>
/// Registo e consulta de ocorrências e de notas partilhadas (F009, RF-090 a RF-095). Não há operações de alteração nem de
/// eliminação: depois de registadas, as ocorrências e as notas são imutáveis (RF-090, RF-091, RNF-049). Cada registo fica
/// disponível a todos os Cuidadores da ficha na consulta seguinte, sem cache (RF-093). O registo de uma ocorrência notifica na
/// aplicação, na mesma transação, todas as pessoas associadas à ficha exceto o autor; o e-mail é da F011 (RF-115).
/// </summary>
public sealed class OcorrenciaService
{
    /// <summary>
    /// Comprimento máximo da descrição de uma ocorrência e do texto de uma nota (modelo de dados, secções 6.20 e 6.21).
    /// </summary>
    public const int ComprimentoMaximoDescricao = 2000;

    private readonly IOcorrenciaRepositorio ocorrencias;
    private readonly INotaRepositorio notas;
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly INotificacaoRepositorio notificacoes;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcorrenciaService"/> class.
    /// </summary>
    /// <param name="ocorrencias">Repositório das ocorrências.</param>
    /// <param name="notas">Repositório das notas.</param>
    /// <param name="utilizadores">Repositório das contas, para o nome do autor.</param>
    /// <param name="notificacoes">Repositório das notificações, para a notificação <c>NovaOcorrencia</c>.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    public OcorrenciaService(
        IOcorrenciaRepositorio ocorrencias,
        INotaRepositorio notas,
        IUtilizadorRepositorio utilizadores,
        INotificacaoRepositorio notificacoes,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.ocorrencias = ocorrencias;
        this.notas = notas;
        this.utilizadores = utilizadores;
        this.notificacoes = notificacoes;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-46: regista uma ocorrência numa ficha ativa, com a descrição e o instante em que aconteceu; o servidor guarda o
    /// autor e o instante do registo (RF-090, RF-092). Só um Cuidador regista. Na mesma transação, cria uma notificação
    /// <c>NovaOcorrencia</c> para cada Cuidador e Familiar da ficha, exceto o autor (RF-115).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha da ocorrência.</param>
    /// <param name="descricao">Descrição da ocorrência.</param>
    /// <param name="ocorreuEmUtc">Instante em que a ocorrência aconteceu, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A ocorrência registada.</returns>
    /// <exception cref="DominioException">Com 404, 403 ou <see cref="CodigosErro.FichaArquivada"/> (409), por esta ordem.</exception>
    /// <exception cref="ValidacaoException">A descrição ou o instante estão em falta ou são inválidos (RNF-028).</exception>
    public async Task<RegistoPartilhado> RegistarOcorrenciaAsync(
        Guid utilizadorId,
        Guid fichaId,
        string? descricao,
        DateTime? ocorreuEmUtc,
        CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validador = new ValidadorDados();
                var texto = ValidadorDados.Normalizar(descricao);
                validador.Obrigatorio("descricao", texto, ComprimentoMaximoDescricao);
                if (ocorreuEmUtc is null)
                {
                    validador.Erro("ocorridaEm", "É obrigatória, com a data, a hora e o desvio, por exemplo 2026-10-07T15:05:00+01:00.");
                }

                validador.LancarSeInvalido();

                var ocorrencia = new Ocorrencia(fichaId, texto!, TruncarAoSegundo(ocorreuEmUtc!.Value), utilizadorId, this.relogio.AgoraUtc());
                this.ocorrencias.Adicionar(ocorrencia);
                var nomeAutor = await this.NomeDaContaAsync(utilizadorId, cancelamento).ConfigureAwait(false);
                this.NotificarNovaOcorrencia(acesso.Ficha, ocorrencia, nomeAutor);
                return new RegistoPartilhado(
                    ocorrencia.Id,
                    ocorrencia.FichaId,
                    TipoRegisto.Ocorrencia,
                    ocorrencia.Descricao,
                    ocorrencia.AutorId,
                    nomeAutor,
                    ocorrencia.OcorreuEmUtc,
                    ocorrencia.RegistadaEmUtc);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-47: regista uma nota dirigida aos Cuidadores de uma ficha ativa; o servidor guarda o autor e o instante do registo
    /// (RF-091, RF-092). Só um Cuidador regista e a nota não gera notificações (RF-115).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha da nota.</param>
    /// <param name="texto">Texto da nota.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A nota registada.</returns>
    /// <exception cref="DominioException">Com 404, 403 ou <see cref="CodigosErro.FichaArquivada"/> (409), por esta ordem.</exception>
    /// <exception cref="ValidacaoException">O texto está em falta ou é demasiado longo (RNF-028).</exception>
    public async Task<RegistoPartilhado> RegistarNotaAsync(Guid utilizadorId, Guid fichaId, string? texto, CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validador = new ValidadorDados();
                var limpo = ValidadorDados.Normalizar(texto);
                validador.Obrigatorio("descricao", limpo, ComprimentoMaximoDescricao);
                validador.LancarSeInvalido();

                var nota = new Nota(fichaId, limpo!, utilizadorId, this.relogio.AgoraUtc());
                this.notas.Adicionar(nota);
                return new RegistoPartilhado(
                    nota.Id,
                    nota.FichaId,
                    TipoRegisto.Nota,
                    nota.Texto,
                    nota.AutorId,
                    await this.NomeDaContaAsync(utilizadorId, cancelamento).ConfigureAwait(false),
                    null,
                    nota.RegistadaEmUtc);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-48: consulta as ocorrências de uma ficha, da mais recente para a mais antiga pelo instante do registo, a Cuidadores e
    /// Familiares associados, também numa ficha arquivada (RF-092 a RF-094).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="de">Primeiro dia, em hora de Lisboa, do registo, ou <see langword="null"/>.</param>
    /// <param name="ate">Último dia, em hora de Lisboa, do registo (inclusivo), ou <see langword="null"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As ocorrências da ficha.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaNaoEncontrada"/> a quem não está associado (RNF-005).</exception>
    public async Task<IReadOnlyList<RegistoPartilhado>> ConsultarOcorrenciasAsync(
        Guid utilizadorId,
        Guid fichaId,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        var (deUtc, ateUtc) = this.Intervalo(de, ate);
        return await this.ocorrencias.DaFichaAsync(fichaId, deUtc, ateUtc, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-49: consulta as notas de uma ficha, da mais recente para a mais antiga pelo instante do registo, só a Cuidadores
    /// associados, também numa ficha arquivada (RF-092, RF-093, RF-095). O Familiar recebe <c>ACESSO_PROIBIDO</c>.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="de">Primeiro dia, em hora de Lisboa, do registo, ou <see langword="null"/>.</param>
    /// <param name="ate">Último dia, em hora de Lisboa, do registo (inclusivo), ou <see langword="null"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As notas da ficha.</returns>
    /// <exception cref="DominioException">Com 404 ou <see cref="CodigosErro.AcessoProibido"/> (403), por esta ordem.</exception>
    public async Task<IReadOnlyList<RegistoPartilhado>> ConsultarNotasAsync(
        Guid utilizadorId,
        Guid fichaId,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirCuidadorAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        var (deUtc, ateUtc) = this.Intervalo(de, ate);
        return await this.notas.DaFichaAsync(fichaId, deUtc, ateUtc, cancelamento).ConfigureAwait(false);
    }

    // A base de dados guarda datetime2(0), que arredonda; o instante é truncado ao segundo antes de gravar (modelo de dados,
    // secção 3).
    private static DateTime TruncarAoSegundo(DateTime instanteUtc)
    {
        return new DateTime(instanteUtc.Ticks - (instanteUtc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    // Os dias de e ate seguem a hora de Lisboa: de começa às 00:00 desse dia e ate inclui o dia inteiro (RNF-047).
    private (DateTime? DeUtc, DateTime? AteUtc) Intervalo(DateOnly? de, DateOnly? ate)
    {
        if (de > ate)
        {
            throw ValidacaoException.DoCampo("ate", "Não pode ser anterior a de.");
        }

        var calendario = this.relogio.Calendario();
        return (
            de is null ? null : calendario.InicioDoDiaUtc(de.Value),
            ate is null ? null : calendario.InicioDoDiaSeguinteUtc(ate.Value));
    }

    // Uma notificação por pessoa associada à ficha (Cuidadores e Familiares), exceto o autor. O instante-limite é o instante do
    // registo, que identifica o evento em UQ_Notificacao_Origem (modelo de dados, secção 6.22).
    private void NotificarNovaOcorrencia(Ficha ficha, Ocorrencia ocorrencia, string nomeAutor)
    {
        var mensagem = TextosOcorrencia.Notificacao(
            ficha.NomeCompleto,
            nomeAutor,
            this.relogio.Calendario().FormatarDataHora(ocorrencia.OcorreuEmUtc));
        foreach (var associacao in ficha.Associacoes.Where(a => a.UtilizadorId != ocorrencia.AutorId))
        {
            this.notificacoes.Adicionar(new Notificacao(
                associacao.UtilizadorId,
                ficha.Id,
                TipoNotificacao.NovaOcorrencia,
                mensagem,
                TipoReferencia.Ocorrencia,
                ocorrencia.Id,
                ocorrencia.RegistadaEmUtc,
                ocorrencia.RegistadaEmUtc));
        }
    }

    private async Task<string> NomeDaContaAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        var conta = await this.utilizadores.ObterPorIdAsync(utilizadorId, cancelamento).ConfigureAwait(false);
        return conta?.Nome ?? string.Empty;
    }
}
