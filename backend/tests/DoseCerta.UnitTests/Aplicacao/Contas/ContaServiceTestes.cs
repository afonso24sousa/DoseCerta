// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Contas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Contas;

/// <summary>
/// Testes unitários das regras de negócio de <see cref="ContaService"/> (F001).
/// </summary>
public class ContaServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string[] CamposDaConta = { "nome", "email" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly HasherSimples hasher = new();
    private readonly ContaService servico;
    private readonly Utilizador ana;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContaServiceTestes"/> class.
    /// </summary>
    public ContaServiceTestes()
    {
        this.servico = new ContaService(this.repositorios, this.hasher, new UnidadeDeTrabalhoEmMemoria());
        this.ana = new Utilizador("Ana", "ana@exemplo.pt", this.hasher.Hash("Segura123"), Agora);
        this.repositorios.Utilizadores.Add(this.ana);
        this.repositorios.Utilizadores.Add(new Utilizador("Rui", "rui@exemplo.pt", this.hasher.Hash("Segura123"), Agora));
    }

    /// <summary>
    /// RF-013: a consulta devolve a conta do Utilizador autenticado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ObterDevolveAConta()
    {
        var conta = await this.servico.ObterAsync(this.ana.Id, CancellationToken.None);

        Assert.Same(this.ana, conta);
    }

    /// <summary>
    /// Uma conta que deixou de existir é tratada como sessão inválida.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ObterContaInexistenteDaNaoAutenticado()
    {
        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(CodigosErro.NaoAutenticado, erro.Codigo);
    }

    /// <summary>
    /// RF-014 CA1: o nome e o e-mail válidos ficam guardados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarGuardaNomeEEmail()
    {
        await this.servico.AtualizarAsync(this.ana.Id, "Ana Costa", "ana.costa@exemplo.pt", CancellationToken.None);

        Assert.Equal("Ana Costa", this.ana.Nome);
        Assert.Equal("ana.costa@exemplo.pt", this.ana.Email);
    }

    /// <summary>
    /// O Utilizador pode manter o seu próprio e-mail ao alterar o nome.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarMantendoOProprioEmailEPermitido()
    {
        await this.servico.AtualizarAsync(this.ana.Id, "Ana Costa", "ANA@exemplo.pt", CancellationToken.None);

        Assert.Equal("Ana Costa", this.ana.Nome);
    }

    /// <summary>
    /// RF-014 CA2: um e-mail de outra conta é recusado e o e-mail anterior mantém-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarComEmailDeOutraContaERecusado()
    {
        var erro = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.AtualizarAsync(this.ana.Id, "Ana", "RUI@exemplo.pt", CancellationToken.None));

        Assert.Equal(CodigosErro.EmailJaRegistado, erro.Codigo);
        Assert.Equal("ana@exemplo.pt", this.ana.Email);
    }

    /// <summary>
    /// RNF-028: dados inválidos na atualização indicam os campos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AtualizarComDadosInvalidosIndicaOsCampos()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.AtualizarAsync(this.ana.Id, new string('a', 121), "invalido", CancellationToken.None));

        Assert.Equal(CamposDaConta, erro.Campos.Select(c => c.Campo));
    }

    /// <summary>
    /// RF-015 CA1: com a palavra-passe atual correta, a nova passa a ser aceite e a anterior deixa de o ser.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AlterarPalavraPasseSubstituiOHash()
    {
        await this.servico.AlterarPalavraPasseAsync(this.ana.Id, "Segura123", "MaisSegura456", CancellationToken.None);

        Assert.True(this.hasher.Verificar(this.ana.PalavraPasseHash, "MaisSegura456"));
        Assert.False(this.hasher.Verificar(this.ana.PalavraPasseHash, "Segura123"));
    }

    /// <summary>
    /// RF-015 CA2: uma nova palavra-passe que não cumpre o RNF-003 é recusada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AlterarPalavraPasseInvalidaERecusada()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.AlterarPalavraPasseAsync(this.ana.Id, "Segura123", "semnumeros", CancellationToken.None));

        Assert.Equal("palavraPasseNova", Assert.Single(erro.Campos).Campo);
    }

    /// <summary>
    /// RF-015 CA3: uma palavra-passe atual incorreta é recusada e a anterior mantém-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AlterarComPalavraPasseAtualErradaERecusado()
    {
        var erro = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.AlterarPalavraPasseAsync(this.ana.Id, "Errada123", "MaisSegura456", CancellationToken.None));

        Assert.Equal(CodigosErro.PalavraPasseAtualIncorreta, erro.Codigo);
        Assert.True(this.hasher.Verificar(this.ana.PalavraPasseHash, "Segura123"));
    }

    /// <summary>
    /// RNF-028: a palavra-passe atual em falta é indicada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AlterarSemPalavraPasseAtualIndicaOCampo()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.AlterarPalavraPasseAsync(this.ana.Id, null, "MaisSegura456", CancellationToken.None));

        Assert.Equal("palavraPasseAtual", Assert.Single(erro.Campos).Campo);
    }
}
