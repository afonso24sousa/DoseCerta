// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Contas;

/// <summary>
/// Criação de conta, início e término de sessão (F001; RF-010, RF-011, RF-012, RF-016, RF-017, RF-044).
/// </summary>
public sealed class AuthService
{
    /// <summary>
    /// Mensagem das credenciais inválidas: igual para e-mail inexistente e palavra-passe errada (RF-012).
    /// </summary>
    public const string MensagemCredenciaisInvalidas = "O e-mail ou a palavra-passe estão incorretos.";

    private readonly IUtilizadorRepositorio utilizadores;
    private readonly ISessaoRevogadaRepositorio sessoesRevogadas;
    private readonly IConviteRepositorio convites;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;
    private readonly Lazy<string> hashDeReferencia;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="utilizadores">Repositório das contas.</param>
    /// <param name="sessoesRevogadas">Repositório das credenciais terminadas.</param>
    /// <param name="convites">Repositório dos convites.</param>
    /// <param name="passwordHasher">Hash das palavras-passe.</param>
    /// <param name="tokenService">Emissão de tokens JWT.</param>
    /// <param name="unidadeDeTrabalho">Unidade de trabalho.</param>
    /// <param name="relogio">Relógio.</param>
    public AuthService(
        IUtilizadorRepositorio utilizadores,
        ISessaoRevogadaRepositorio sessoesRevogadas,
        IConviteRepositorio convites,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.utilizadores = utilizadores;
        this.sessoesRevogadas = sessoesRevogadas;
        this.convites = convites;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;

        // Hash usado quando o e-mail não existe, para que a resposta demore o mesmo tempo (RF-012).
        this.hashDeReferencia = new Lazy<string>(() => passwordHasher.Hash("referencia-sem-conta-1"));
    }

    /// <summary>
    /// Cria uma conta (OP-01). O registo não inicia sessão.
    /// </summary>
    /// <param name="nome">Nome da pessoa.</param>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="palavraPasse">Palavra-passe.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A conta criada e o número de convites pendentes para o e-mail.</returns>
    /// <exception cref="ValidacaoException">Dados em falta ou inválidos.</exception>
    /// <exception cref="DominioException">O e-mail já está registado (<c>EMAIL_JA_REGISTADO</c>).</exception>
    public async Task<ResultadoRegisto> RegistarAsync(string? nome, string? email, string? palavraPasse, CancellationToken cancelamento)
    {
        var nomeLimpo = ValidadorDados.Normalizar(nome);
        var emailLimpo = ValidadorDados.Normalizar(email);
        new ValidadorDados()
            .Obrigatorio("nome", nomeLimpo, 120)
            .Email("email", emailLimpo)
            .PalavraPasse("palavraPasse", palavraPasse)
            .LancarSeInvalido();

        var utilizador = await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                if (await this.utilizadores.ExisteEmailAsync(emailLimpo!, null, cancelamento).ConfigureAwait(false))
                {
                    throw new DominioException(CodigosErro.EmailJaRegistado, "Já existe uma conta com o e-mail indicado.");
                }

                var novo = new Utilizador(nomeLimpo!, emailLimpo!, this.passwordHasher.Hash(palavraPasse!), this.relogio.AgoraUtc());
                this.utilizadores.Adicionar(novo);
                return novo;
            },
            cancelamento).ConfigureAwait(false);

        var pendentes = await this.convites.ContarPendentesPorEmailAsync(utilizador.Email, cancelamento).ConfigureAwait(false);
        return new ResultadoRegisto(utilizador, pendentes);
    }

    /// <summary>
    /// Inicia sessão e emite um token JWT com validade de 8 horas (OP-02).
    /// </summary>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="palavraPasse">Palavra-passe.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O token de sessão.</returns>
    /// <exception cref="ValidacaoException">Campos em falta.</exception>
    /// <exception cref="DominioException">Credenciais inválidas (<c>CREDENCIAIS_INVALIDAS</c>).</exception>
    public async Task<TokenSessao> AutenticarAsync(string? email, string? palavraPasse, CancellationToken cancelamento)
    {
        var emailLimpo = ValidadorDados.Normalizar(email);
        var validador = new ValidadorDados();
        if (emailLimpo is null)
        {
            validador.Erro("email", "É obrigatório.");
        }

        if (string.IsNullOrEmpty(palavraPasse))
        {
            validador.Erro("palavraPasse", "É obrigatória.");
        }

        validador.LancarSeInvalido();

        var utilizador = await this.utilizadores.ObterPorEmailAsync(emailLimpo!, cancelamento).ConfigureAwait(false);
        var hash = utilizador?.PalavraPasseHash ?? this.hashDeReferencia.Value;
        var valida = this.passwordHasher.Verificar(hash, palavraPasse!);

        if (utilizador is null || !valida)
        {
            throw new DominioException(CodigosErro.CredenciaisInvalidas, MensagemCredenciaisInvalidas);
        }

        return this.tokenService.Emitir(utilizador, this.relogio.AgoraUtc());
    }

    /// <summary>
    /// Termina a sessão: regista a credencial, que deixa de ser aceite (OP-03, RF-016).
    /// </summary>
    /// <param name="jti">Identificador da credencial.</param>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="expiraEmUtc">Instante de expiração do token, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa que termina quando a sessão estiver terminada.</returns>
    public async Task TerminarSessaoAsync(Guid jti, Guid utilizadorId, DateTime expiraEmUtc, CancellationToken cancelamento)
    {
        await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                if (!await this.sessoesRevogadas.EstaRevogadaAsync(jti, cancelamento).ConfigureAwait(false))
                {
                    this.sessoesRevogadas.Adicionar(new SessaoRevogada(jti, utilizadorId, this.relogio.AgoraUtc(), expiraEmUtc));
                }

                return true;
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Indica se a credencial continua aceite, isto é, se a sessão não foi terminada (RF-016, RF-017).
    /// </summary>
    /// <param name="jti">Identificador da credencial.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se a credencial for aceite.</returns>
    public async Task<bool> CredencialAceiteAsync(Guid jti, CancellationToken cancelamento)
    {
        return !await this.sessoesRevogadas.EstaRevogadaAsync(jti, cancelamento).ConfigureAwait(false);
    }
}
