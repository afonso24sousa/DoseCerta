// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Compromissos;

/// <summary>
/// Registo, consulta, edição e remoção dos compromissos de saúde de uma ficha (F008, RF-080 a RF-082, RF-116). Os compromissos
/// não são confirmáveis (RF-087). O aviso antecipado é enviado pelo processo periódico (F011).
/// </summary>
public sealed class CompromissoService
{
    /// <summary>
    /// Comprimento máximo do local (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoLocal = 150;

    /// <summary>
    /// Comprimento máximo do motivo (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoMotivo = 250;

    private const string MensagemNaoEncontrado = "O compromisso não existe ou pertence a uma ficha a que não está associado.";

    private readonly ICompromissoRepositorio compromissos;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissoService"/> class.
    /// </summary>
    /// <param name="compromissos">Repositório dos compromissos.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema, com o calendário de Lisboa.</param>
    public CompromissoService(
        ICompromissoRepositorio compromissos,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.compromissos = compromissos;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-41: regista um compromisso numa ficha ativa (RF-080, RF-116). Só um Cuidador regista.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha do compromisso.</param>
    /// <param name="dados">Dados do compromisso.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O compromisso registado.</returns>
    public async Task<Compromisso> RegistarAsync(Guid utilizadorId, Guid fichaId, DadosCompromisso dados, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validados = Validar(dados);
                var compromisso = new Compromisso(
                    fichaId,
                    validados.DataHoraUtc,
                    validados.Local,
                    validados.Motivo,
                    validados.AntecedenciaMin,
                    utilizadorId,
                    this.relogio.AgoraUtc());
                this.compromissos.Adicionar(compromisso);
                return compromisso;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-42: lista os compromissos de uma ficha a Cuidadores e Familiares associados, também numa ficha arquivada, por ordem
    /// da data e hora (RF-080, RF-116).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="de">Primeiro dia, em hora de Lisboa, inclusive; opcional.</param>
    /// <param name="ate">Último dia, em hora de Lisboa, inclusive; opcional.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos da ficha.</returns>
    /// <exception cref="ValidacaoException">Se <paramref name="ate"/> for anterior a <paramref name="de"/>.</exception>
    public async Task<IReadOnlyList<Compromisso>> ListarAsync(
        Guid utilizadorId,
        Guid fichaId,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancelamento)
    {
        if (de is not null && ate < de)
        {
            throw ValidacaoException.DoCampo("ate", "Não pode ser anterior à data de início (de).");
        }

        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        var calendario = this.relogio.Calendario();
        DateTime? deUtc = de is DateOnly inicio ? calendario.InicioDoDiaUtc(inicio) : null;
        DateTime? ateUtc = ate is DateOnly fim ? calendario.InicioDoDiaSeguinteUtc(fim) : null;
        return await this.compromissos.DaFichaAsync(fichaId, deUtc, ateUtc, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-43: edita um compromisso de uma ficha ativa; o aviso passa a ser calculado com os valores novos (RF-081, RF-117).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="compromissoId">Compromisso a editar.</param>
    /// <param name="dados">Dados novos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O compromisso editado.</returns>
    public async Task<Compromisso> EditarAsync(Guid utilizadorId, Guid compromissoId, DadosCompromisso dados, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(dados);

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var compromisso = await this.ObterComAcessoAsync(utilizadorId, compromissoId, cancelamento).ConfigureAwait(false);
                var validados = Validar(dados);
                compromisso.Atualizar(validados.DataHoraUtc, validados.Local, validados.Motivo, validados.AntecedenciaMin);
                return compromisso;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-44: remove um compromisso de uma ficha ativa; deixa de aparecer na agenda e o aviso deixa de ser emitido (RF-082, RF-117).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="compromissoId">Compromisso a remover.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task RemoverAsync(Guid utilizadorId, Guid compromissoId, CancellationToken cancelamento)
    {
        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var compromisso = await this.ObterComAcessoAsync(utilizadorId, compromissoId, cancelamento).ConfigureAwait(false);
                this.compromissos.Remover(compromisso);
                return true;
            },
            cancelamento).ConfigureAwait(false);
    }

    // Data e hora, local e motivo obrigatórios; a antecedência, se indicada, tem de ser um dos valores permitidos (RF-080,
    // RF-116, RNF-028). O instante fica ao segundo, como na base de dados.
    private static DadosValidados Validar(DadosCompromisso dados)
    {
        var validador = new ValidadorDados();
        if (dados.DataHora is null)
        {
            validador.Erro("dataHora", "É obrigatória, no formato ISO 8601 com o desvio, por exemplo 2026-10-12T10:30:00+01:00.");
        }

        var local = ValidadorDados.Normalizar(dados.Local);
        validador.Obrigatorio("local", local, ComprimentoMaximoLocal);
        var motivo = ValidadorDados.Normalizar(dados.Motivo);
        validador.Obrigatorio("motivo", motivo, ComprimentoMaximoMotivo);

        var antecedencia = dados.AntecedenciaAvisoMinutos ?? ParametrosDeNegocio.AntecedenciaPorOmissaoMin;
        if (!ParametrosDeNegocio.AntecedenciaValida(antecedencia))
        {
            validador.Erro("antecedenciaAvisoMinutos", "Tem de ser 30, 60, 120, 1440 ou 2880 (30 minutos, 1, 2, 24 ou 48 horas).");
        }

        validador.LancarSeInvalido();
        var utc = dados.DataHora!.Value.UtcDateTime;
        var aoSegundo = new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        return new DadosValidados(aoSegundo, local!, motivo!, antecedencia);
    }

    // Compromisso inexistente ou de ficha não associada: RECURSO_NAO_ENCONTRADO (RNF-005); Familiar: ACESSO_PROIBIDO;
    // ficha arquivada: FICHA_ARQUIVADA (RF-120).
    private async Task<Compromisso> ObterComAcessoAsync(Guid utilizadorId, Guid compromissoId, CancellationToken cancelamento)
    {
        var compromisso = await this.compromissos.ObterPorIdAsync(compromissoId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrado);
        try
        {
            await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, compromisso.FichaId, cancelamento).ConfigureAwait(false);
        }
        catch (DominioException erro) when (erro.Codigo == CodigosErro.FichaNaoEncontrada)
        {
            throw new DominioException(CodigosErro.RecursoNaoEncontrado, MensagemNaoEncontrado);
        }

        return compromisso;
    }

    private sealed record DadosValidados(DateTime DataHoraUtc, string Local, string Motivo, int AntecedenciaMin);
}
