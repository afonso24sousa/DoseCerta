// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Pessoas;

/// <summary>
/// Gestão das pessoas associadas a uma ficha: listar, alterar o papel, remover o acesso e transferir o Cuidador principal (F004).
/// As regras dos papéis são aplicadas pela <see cref="Ficha"/>; cada escrita corre numa só transação (RF-049, RF-130 a RF-134, RNF-046).
/// </summary>
public sealed class PessoasService
{
    private readonly IFichaRepositorio fichas;
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasService"/> class.
    /// </summary>
    /// <param name="fichas">Repositório das fichas.</param>
    /// <param name="utilizadores">Repositório das contas, para o nome e o e-mail das pessoas.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    public PessoasService(
        IFichaRepositorio fichas,
        IUtilizadorRepositorio utilizadores,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        this.fichas = fichas;
        this.utilizadores = utilizadores;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// OP-18: lista os Cuidadores e os Familiares da ficha, com o papel e a indicação do Cuidador principal e se é provisório (RF-025).
    /// Qualquer pessoa associada a consulta, também numa ficha arquivada (DP-03, RP-13).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As pessoas associadas, com o Cuidador principal primeiro.</returns>
    public async Task<IReadOnlyList<PessoaAssociada>> ListarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        return await this.fichas.ListarPessoasAsync(fichaId, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-23 e OP-24: promove um Familiar a Cuidador ou despromove um Cuidador a Familiar, numa ficha ativa, a pedido de um Cuidador.
    /// O novo papel tem efeito no pedido seguinte, porque o papel é lido da base de dados em cada pedido (RF-049, RF-130, RP-01).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="alvoId">Pessoa cujo papel muda.</param>
    /// <param name="novoPapel">Papel pretendido: <see cref="Papel.Cuidador"/> ou <see cref="Papel.Familiar"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A pessoa, com o papel pretendido.</returns>
    /// <exception cref="ValidacaoException">Se o papel faltar ou não for Cuidador nem Familiar.</exception>
    public async Task<PessoaAssociada> AlterarPapelAsync(
        Guid utilizadorId,
        Guid fichaId,
        Guid alvoId,
        Papel? novoPapel,
        CancellationToken cancelamento)
    {
        if (novoPapel is not Papel papel || !Enum.IsDefined(papel))
        {
            throw ValidacaoException.DoCampo("papel", "É obrigatório e tem de ser Cuidador ou Familiar.");
        }

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var associacao = acesso.Ficha.AlterarPapel(alvoId, papel);
                return await this.PessoaAsync(associacao, cancelamento).ConfigureAwait(false);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-25: remove o acesso de uma pessoa a uma ficha ativa, a pedido de um Cuidador, incluindo o seu próprio acesso.
    /// A pessoa removida passa a receber 404 nessa ficha a partir do pedido seguinte (RF-131, RNF-005). O Familiar não remove
    /// ninguém, nem a si próprio (DP-01).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="alvoId">Pessoa cujo acesso é removido.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task RemoverAcessoAsync(Guid utilizadorId, Guid fichaId, Guid alvoId, CancellationToken cancelamento)
    {
        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                acesso.Ficha.RemoverAcesso(alvoId);
                return true;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-26: atribui o papel de Cuidador principal a outro Cuidador de uma ficha ativa, a pedido de qualquer Cuidador; o anterior
    /// continua Cuidador e a ficha fica com exatamente um Cuidador principal, numa só transação (RF-132, RNF-046, DP-02).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="novoPrincipalId">Cuidador que passa a Cuidador principal.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O novo Cuidador principal.</returns>
    /// <exception cref="ValidacaoException">Se o destinatário não for indicado.</exception>
    public async Task<PessoaAssociada> TransferirPrincipalAsync(
        Guid utilizadorId,
        Guid fichaId,
        Guid? novoPrincipalId,
        CancellationToken cancelamento)
    {
        if (novoPrincipalId is not Guid destinatario || destinatario == Guid.Empty)
        {
            throw ValidacaoException.DoCampo("utilizadorId", "É obrigatório: identificador do Cuidador que passa a Cuidador principal.");
        }

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var novo = await acesso.Ficha
                    .TransferirPrincipalAsync(destinatario, () => this.unidadeDeTrabalho.GravarAlteracoesAsync(cancelamento))
                    .ConfigureAwait(false);
                return await this.PessoaAsync(novo, cancelamento).ConfigureAwait(false);
            },
            cancelamento).ConfigureAwait(false);
    }

    private async Task<PessoaAssociada> PessoaAsync(Associacao associacao, CancellationToken cancelamento)
    {
        var utilizador = await this.utilizadores.ObterPorIdAsync(associacao.UtilizadorId, cancelamento).ConfigureAwait(false)
            ?? throw new InvalidOperationException("A pessoa associada não tem conta.");
        return new PessoaAssociada(
            utilizador.Id,
            utilizador.Nome,
            utilizador.Email,
            associacao.Papel,
            associacao.EPrincipal,
            associacao.EProvisorio);
    }
}
