// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Contas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Contas;

/// <summary>
/// Testes unitários das regras de negócio de <see cref="AuthService"/> (F001).
/// </summary>
public class AuthServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string[] CamposDoRegisto = { "nome", "email", "palavraPasse" };
    private static readonly string[] CamposDoLogin = { "email", "palavraPasse" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidade = new();
    private readonly AuthService servico;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthServiceTestes"/> class.
    /// </summary>
    public AuthServiceTestes()
    {
        this.servico = new AuthService(
            this.repositorios,
            this.repositorios,
            this.repositorios,
            new HasherSimples(),
            new TokensFalsos(),
            this.unidade,
            new RelogioFixo(Agora));
    }

    /// <summary>
    /// RF-010 CA1: o registo com dados válidos cria a conta, com a palavra-passe só em hash (RNF-001).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComDadosValidosCriaAConta()
    {
        var resultado = await this.servico.RegistarAsync("  Ana Fictícia ", "ana@exemplo.pt", "Segura123", CancellationToken.None);

        var conta = Assert.Single(this.repositorios.Utilizadores);
        Assert.Same(conta, resultado.Utilizador);
        Assert.Equal("Ana Fictícia", conta.Nome);
        Assert.Equal("ana@exemplo.pt", conta.Email);
        Assert.NotEqual("Segura123", conta.PalavraPasseHash);
        Assert.Equal(Agora, conta.CriadoEmUtc);
        Assert.Equal(1, this.unidade.Confirmadas);
    }

    /// <summary>
    /// RF-010 CA2 e RNF-028: campos em falta ou inválidos são todos indicados e a conta não é criada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComDadosInvalidosIndicaOsCampos()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.RegistarAsync(" ", "nao-e-email", "curta", CancellationToken.None));

        Assert.Equal(CamposDoRegisto, erro.Campos.Select(c => c.Campo));
        Assert.Empty(this.repositorios.Utilizadores);
    }

    /// <summary>
    /// RF-011: um e-mail já registado, mesmo com maiúsculas diferentes, é recusado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarComEmailJaRegistadoERecusado()
    {
        await this.servico.RegistarAsync("Ana", "ana@exemplo.pt", "Segura123", CancellationToken.None);

        var erro = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.RegistarAsync("Outra Ana", "ANA@exemplo.pt", "Segura123", CancellationToken.None));

        Assert.Equal(CodigosErro.EmailJaRegistado, erro.Codigo);
        Assert.Single(this.repositorios.Utilizadores);
    }

    /// <summary>
    /// RF-044: o registo indica os convites pendentes dirigidos ao e-mail da conta nova.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarIndicaOsConvitesPendentes()
    {
        this.repositorios.Convites.Add(new Convite(Guid.NewGuid(), "joao@exemplo.pt", Papel.Familiar, false, Guid.NewGuid(), Agora));

        var resultado = await this.servico.RegistarAsync("João", "joao@exemplo.pt", "Segura123", CancellationToken.None);

        Assert.Equal(1, resultado.ConvitesPendentes);
    }

    /// <summary>
    /// RF-012 CA1: credenciais corretas emitem um token válido por 8 horas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AutenticarComCredenciaisCorretasEmiteToken()
    {
        await this.servico.RegistarAsync("Ana", "ana@exemplo.pt", "Segura123", CancellationToken.None);

        var sessao = await this.servico.AutenticarAsync("ANA@exemplo.pt", "Segura123", CancellationToken.None);

        Assert.Equal(Agora.AddHours(8), sessao.ExpiraEmUtc);
        Assert.NotEqual(Guid.Empty, sessao.Jti);
    }

    /// <summary>
    /// RF-012 CA2: e-mail inexistente e palavra-passe errada dão o mesmo erro e a mesma mensagem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AutenticarComCredenciaisErradasNaoIndicaQualEstaErrado()
    {
        await this.servico.RegistarAsync("Ana", "ana@exemplo.pt", "Segura123", CancellationToken.None);

        var semConta = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.AutenticarAsync("ninguem@exemplo.pt", "Segura123", CancellationToken.None));
        var errada = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.AutenticarAsync("ana@exemplo.pt", "Errada123", CancellationToken.None));

        Assert.Equal(CodigosErro.CredenciaisInvalidas, semConta.Codigo);
        Assert.Equal(semConta.Codigo, errada.Codigo);
        Assert.Equal(semConta.Message, errada.Message);
    }

    /// <summary>
    /// RNF-028: o início de sessão sem campos indica os campos em falta.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AutenticarSemCamposIndicaOsCampos()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.AutenticarAsync(null, string.Empty, CancellationToken.None));

        Assert.Equal(CamposDoLogin, erro.Campos.Select(c => c.Campo));
    }

    /// <summary>
    /// RF-016: terminar a sessão regista a credencial, que deixa de ser aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TerminarSessaoRevogaACredencial()
    {
        var jti = Guid.NewGuid();
        Assert.True(await this.servico.CredencialAceiteAsync(jti, CancellationToken.None));

        await this.servico.TerminarSessaoAsync(jti, Guid.NewGuid(), Agora.AddHours(8), CancellationToken.None);
        await this.servico.TerminarSessaoAsync(jti, Guid.NewGuid(), Agora.AddHours(8), CancellationToken.None);

        Assert.False(await this.servico.CredencialAceiteAsync(jti, CancellationToken.None));
        var sessao = Assert.Single(this.repositorios.Sessoes);
        Assert.Equal(Agora, sessao.RevogadaEmUtc);
    }

    /// <summary>
    /// As credenciais terminadas expiradas podem ser eliminadas.
    /// </summary>
    [Fact]
    public void SessaoRevogadaIndicaQuandoExpirou()
    {
        var sessao = new SessaoRevogada(Guid.NewGuid(), Guid.NewGuid(), Agora, Agora.AddHours(1));

        Assert.False(sessao.Expirada(Agora));
        Assert.True(sessao.Expirada(Agora.AddHours(1)));
    }
}
