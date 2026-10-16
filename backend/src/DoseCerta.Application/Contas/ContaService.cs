// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Contas;

/// <summary>
/// Consulta e atualização dos dados da conta e alteração da palavra-passe (F001; RF-013, RF-014, RF-015).
/// </summary>
public sealed class ContaService
{
    private readonly IUtilizadorRepositorio utilizadores;
    private readonly IPasswordHasher passwordHasher;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContaService"/> class.
    /// </summary>
    /// <param name="utilizadores">Repositório das contas.</param>
    /// <param name="passwordHasher">Hash das palavras-passe.</param>
    /// <param name="unidadeDeTrabalho">Unidade de trabalho.</param>
    public ContaService(IUtilizadorRepositorio utilizadores, IPasswordHasher passwordHasher, IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        this.utilizadores = utilizadores;
        this.passwordHasher = passwordHasher;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Obtém os dados da conta do Utilizador autenticado (OP-04).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador autenticado.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A conta.</returns>
    /// <exception cref="DominioException">A conta deixou de existir (<c>NAO_AUTENTICADO</c>).</exception>
    public async Task<Utilizador> ObterAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        return await this.utilizadores.ObterPorIdAsync(utilizadorId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.NaoAutenticado, "A sessão não é válida ou expirou. Inicie sessão novamente.");
    }

    /// <summary>
    /// Altera o nome e o e-mail da conta (OP-05). Se o novo e-mail pertencer a outra conta, a alteração é recusada.
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador autenticado.</param>
    /// <param name="nome">Novo nome.</param>
    /// <param name="email">Novo e-mail.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A conta atualizada.</returns>
    /// <exception cref="ValidacaoException">Dados em falta ou inválidos.</exception>
    /// <exception cref="DominioException">O e-mail pertence a outra conta (<c>EMAIL_JA_REGISTADO</c>).</exception>
    public async Task<Utilizador> AtualizarAsync(Guid utilizadorId, string? nome, string? email, CancellationToken cancelamento)
    {
        var nomeLimpo = ValidadorDados.Normalizar(nome);
        var emailLimpo = ValidadorDados.Normalizar(email);
        new ValidadorDados()
            .Obrigatorio("nome", nomeLimpo, 120)
            .Email("email", emailLimpo)
            .LancarSeInvalido();

        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var utilizador = await this.ObterAsync(utilizadorId, cancelamento).ConfigureAwait(false);
                if (await this.utilizadores.ExisteEmailAsync(emailLimpo!, utilizadorId, cancelamento).ConfigureAwait(false))
                {
                    throw new DominioException(CodigosErro.EmailJaRegistado, "O novo e-mail pertence a outra conta; o e-mail anterior mantém-se.");
                }

                utilizador.AtualizarDados(nomeLimpo!, emailLimpo!);
                return utilizador;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Altera a palavra-passe, exigindo a atual (OP-06). Depois da alteração, a anterior deixa de ser aceite.
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador autenticado.</param>
    /// <param name="palavraPasseAtual">Palavra-passe atual.</param>
    /// <param name="palavraPasseNova">Nova palavra-passe.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa que termina quando a palavra-passe estiver alterada.</returns>
    /// <exception cref="ValidacaoException">Campos em falta ou nova palavra-passe inválida.</exception>
    /// <exception cref="DominioException">Palavra-passe atual errada (<c>PALAVRA_PASSE_ATUAL_INCORRETA</c>).</exception>
    public async Task AlterarPalavraPasseAsync(Guid utilizadorId, string? palavraPasseAtual, string? palavraPasseNova, CancellationToken cancelamento)
    {
        var validador = new ValidadorDados();
        if (string.IsNullOrEmpty(palavraPasseAtual))
        {
            validador.Erro("palavraPasseAtual", "É obrigatória.");
        }

        validador.PalavraPasse("palavraPasseNova", palavraPasseNova).LancarSeInvalido();

        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                var utilizador = await this.ObterAsync(utilizadorId, cancelamento).ConfigureAwait(false);
                if (!this.passwordHasher.Verificar(utilizador.PalavraPasseHash, palavraPasseAtual!))
                {
                    throw new DominioException(
                        CodigosErro.PalavraPasseAtualIncorreta,
                        "A palavra-passe atual está incorreta; a palavra-passe anterior mantém-se.");
                }

                utilizador.AlterarPalavraPasse(this.passwordHasher.Hash(palavraPasseNova!));
                return true;
            },
            cancelamento).ConfigureAwait(false);
    }
}
