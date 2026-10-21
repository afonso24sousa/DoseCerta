// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Convites;

/// <summary>
/// Criação e resposta a convites para uma ficha (F004, RF-040 a RF-046). Cada escrita corre numa só transação (RNF-046).
/// </summary>
public sealed class ConviteService
{
    private readonly IConviteRepositorio convites;
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConviteService"/> class.
    /// </summary>
    /// <param name="convites">Repositório dos convites.</param>
    /// <param name="utilizadores">Repositório das contas.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    public ConviteService(
        IConviteRepositorio convites,
        IUtilizadorRepositorio utilizadores,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.convites = convites;
        this.utilizadores = utilizadores;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-19: um Cuidador de uma ficha ativa convida uma pessoa pelo e-mail, como Cuidador ou Familiar, ou para Cuidador principal
    /// (RF-025, RF-040, RF-043). O convite fica pendente e não dá acesso à ficha até ser aceite (RP-09).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="email">E-mail da pessoa convidada.</param>
    /// <param name="papel">Papel oferecido.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal; o papel tem de ser Cuidador.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O convite criado.</returns>
    /// <exception cref="ValidacaoException">Se o e-mail ou o papel faltarem ou forem inválidos.</exception>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.CuidadorPrincipalProtegido"/>, <see cref="CodigosErro.PessoaJaAssociada"/> ou
    /// <see cref="CodigosErro.ConvitePendenteExiste"/>, além dos erros de <see cref="AutorizacaoFichaService.ExigirCuidadorDeFichaAtivaAsync"/>.
    /// </exception>
    public async Task<ConviteResumo> ConvidarAsync(
        Guid utilizadorId,
        Guid fichaId,
        string? email,
        Papel? papel,
        bool paraPrincipal,
        CancellationToken cancelamento)
    {
        var emailLimpo = ValidadorDados.Normalizar(email);
        var validador = new ValidadorDados().Email("email", emailLimpo);
        if (papel is not Papel papelOferecido || !Enum.IsDefined(papelOferecido))
        {
            validador.Erro("papel", "É obrigatório e tem de ser Cuidador ou Familiar.");
        }
        else if (paraPrincipal && papelOferecido != Papel.Cuidador)
        {
            validador.Erro("papel", "Tem de ser Cuidador num convite para Cuidador principal (paraPrincipal=true).");
        }

        validador.LancarSeInvalido();

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var acesso = await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var ficha = acesso.Ficha;
                if (paraPrincipal)
                {
                    ficha.ExigirPrincipalTransferivel();
                }

                var conta = await this.utilizadores.ObterPorEmailAsync(emailLimpo!, cancelamento).ConfigureAwait(false);
                if (conta is not null)
                {
                    ficha.ExigirQueNaoEstaAssociado(conta.Id);
                }

                if (await this.convites.ExistePendenteAsync(fichaId, emailLimpo!, cancelamento).ConfigureAwait(false))
                {
                    throw new DominioException(
                        CodigosErro.ConvitePendenteExiste,
                        "Já existe um convite pendente para este e-mail nesta ficha; o convite não foi criado.");
                }

                if (paraPrincipal)
                {
                    ficha.MarcarPrincipalProvisorio();
                }

                var convite = new Convite(fichaId, emailLimpo!, papel!.Value, paraPrincipal, utilizadorId, this.relogio.AgoraUtc());
                this.convites.Adicionar(convite);
                var quemConvida = await this.utilizadores.ObterPorIdAsync(utilizadorId, cancelamento).ConfigureAwait(false);
                return Resumo(convite, ficha, quemConvida?.Nome ?? string.Empty);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-20: lista os convites pendentes dirigidos ao e-mail da conta autenticada, com a ficha, o papel oferecido e quem convidou
    /// (RF-042). Inclui os convites criados antes do registo da conta (RF-044) e aqueles cujo e-mail não foi entregue (RF-135),
    /// porque o convite se liga à conta pelo e-mail e não depende do estado do envio.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os convites pendentes, do mais recente para o mais antigo.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.NaoAutenticado"/>, se a conta do token já não existir.</exception>
    public async Task<IReadOnlyList<ConviteResumo>> ListarRecebidosAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        var conta = await this.utilizadores.ObterPorIdAsync(utilizadorId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.NaoAutenticado, "A sessão não é válida ou expirou. Inicie sessão novamente.");
        return await this.convites.PendentesPorEmailAsync(conta.Email, cancelamento).ConfigureAwait(false);
    }

    private static ConviteResumo Resumo(Convite convite, Ficha ficha, string convidadoPor)
    {
        return new ConviteResumo(
            convite.Id,
            convite.FichaId,
            ficha.NomeCompleto,
            convite.PapelOferecido,
            convite.ParaPrincipal,
            convidadoPor,
            convite.Estado,
            convite.CriadoEmUtc);
    }
}
