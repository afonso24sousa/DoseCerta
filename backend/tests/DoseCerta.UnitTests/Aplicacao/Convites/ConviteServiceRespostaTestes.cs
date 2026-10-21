// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Pessoas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Convites;

/// <summary>
/// Testes unitários da aceitação e da recusa de convites no <see cref="ConviteService"/> (F004; OP-21, OP-22; RF-026, RF-027, RF-045, RF-046).
/// </summary>
public class ConviteServiceRespostaTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora.AddDays(1);

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly ConviteService servico;
    private readonly Utilizador cuidador;
    private readonly Utilizador convidado;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConviteServiceRespostaTestes"/> class.
    /// </summary>
    public ConviteServiceRespostaTestes()
    {
        this.servico = ServicosDeTeste.ConviteService(this.repositorios, this.unidadeDeTrabalho, new RelogioFixo(Agora));
        this.cuidador = this.NovoUtilizador("Zé Cuidador", "ze@exemplo.pt");
        this.convidado = this.NovoUtilizador("Rui Convidado", "rui@exemplo.pt");
        this.ficha = FichasDeTeste.DeTerceiro(this.cuidador.Id);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-045 CA1: aceitar um convite para Familiar associa o convidado com esse papel e o convite deixa de estar pendente.
    /// O convite é para o e-mail com maiúsculas diferentes, como a collation da coluna permite.
    /// </summary>
    /// <param name="papel">Papel oferecido.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(Papel.Familiar)]
    [InlineData(Papel.Cuidador)]
    public async Task AceitarAssociaComOPapelOferecido(Papel papel)
    {
        var convite = this.NovoConvite("RUI@exemplo.pt", papel);

        var pessoa = await this.servico.AceitarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        Assert.Equal(new PessoaAssociada(this.convidado.Id, "Rui Convidado", "rui@exemplo.pt", papel, false, false), pessoa);
        Assert.Equal(papel, this.ficha.ObterAssociacao(this.convidado.Id)!.Papel);
        Assert.Equal(EstadoConvite.Aceite, convite.Estado);
        Assert.Equal(this.convidado.Id, convite.UtilizadorConvidadoId);
        Assert.Equal(Agora, convite.RespondidoEmUtc);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-046 CA1: recusar não associa o convidado e o convite deixa de estar pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RecusarNaoAssocia()
    {
        var convite = this.NovoConvite("rui@exemplo.pt", Papel.Familiar);

        await this.servico.RecusarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        Assert.Null(this.ficha.ObterAssociacao(this.convidado.Id));
        Assert.Equal(EstadoConvite.Recusado, convite.Estado);
        Assert.Equal(this.convidado.Id, convite.UtilizadorConvidadoId);
    }

    /// <summary>
    /// UC-14 E2: um convite que não existe, ou que é dirigido a outro e-mail, dá a mesma resposta 404, ao aceitar e ao recusar.
    /// </summary>
    /// <param name="aceitar">Indica se a resposta é aceitar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConviteInexistenteOuDeOutroEmailNaoEEncontrado(bool aceitar)
    {
        var deOutro = this.NovoConvite("rita@exemplo.pt", Papel.Familiar);

        var paraOutro = await Assert.ThrowsAsync<DominioException>(() => this.Responder(aceitar, deOutro.Id));
        var inexistente = await Assert.ThrowsAsync<DominioException>(() => this.Responder(aceitar, Guid.NewGuid()));

        Assert.Equal(CodigosErro.ConviteNaoEncontrado, paraOutro.Codigo);
        Assert.Equal(CodigosErro.ConviteNaoEncontrado, inexistente.Codigo);
        Assert.Equal(paraOutro.Message, inexistente.Message);
        Assert.Equal(EstadoConvite.Pendente, deOutro.Estado);
    }

    /// <summary>
    /// UC-14 E1: um convite já respondido não é respondido outra vez (409), seja qual for a segunda resposta.
    /// </summary>
    /// <param name="aceitar">Indica se a segunda resposta é aceitar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConviteJaRespondidoDa409(bool aceitar)
    {
        var convite = this.NovoConvite("rui@exemplo.pt", Papel.Familiar);
        await this.servico.RecusarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.Responder(aceitar, convite.Id));

        Assert.Equal(CodigosErro.ConviteJaRespondido, erro.Codigo);
        Assert.Equal(EstadoConvite.Recusado, convite.Estado);
        Assert.Null(this.ficha.ObterAssociacao(this.convidado.Id));
    }

    /// <summary>
    /// D4: se o convidado já estiver associado à ficha (por exemplo, por ter mudado o e-mail da conta), aceitar dá 409
    /// <c>PESSOA_JA_ASSOCIADA</c> e o papel não muda.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvidadoJaAssociadoNaoAceita()
    {
        this.ficha.Associar(this.convidado.Id, Papel.Familiar, FichasDeTeste.Agora);
        var convite = this.NovoConvite("rui@exemplo.pt", Papel.Cuidador);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.AceitarAsync(this.convidado.Id, convite.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.PessoaJaAssociada, erro.Codigo);
        Assert.Equal(Papel.Familiar, this.ficha.ObterAssociacao(this.convidado.Id)!.Papel);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// UC-14 A5, RP-13: numa ficha arquivada depois do convite, o convidado aceita na mesma.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaAceitaNaMesma()
    {
        var convite = this.NovoConvite("rui@exemplo.pt", Papel.Familiar);
        this.ficha.Arquivar(Agora);

        await this.servico.AceitarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        Assert.NotNull(this.ficha.ObterAssociacao(this.convidado.Id));
    }

    /// <summary>
    /// RF-026 CA1: aceitar o convite para Cuidador principal deixa o convidado como Cuidador principal e retira o criador provisório.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AceitarConviteParaPrincipalTransfereOPapel()
    {
        var convite = this.ConviteParaPrincipal();

        var pessoa = await this.servico.AceitarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        Assert.True(pessoa.Principal);
        Assert.False(pessoa.Provisorio);
        Assert.Equal(Papel.Cuidador, pessoa.Papel);
        Assert.Null(this.ficha.ObterAssociacao(this.cuidador.Id));
        Assert.Equal(EstadoConvite.Aceite, convite.Estado);
    }

    /// <summary>
    /// RF-027 CA1: recusar o convite para Cuidador principal mantém o criador como Cuidador principal, já não provisório.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RecusarConviteParaPrincipalMantemOCriador()
    {
        var convite = this.ConviteParaPrincipal();

        await this.servico.RecusarAsync(this.convidado.Id, convite.Id, CancellationToken.None);

        var criador = this.ficha.ObterAssociacao(this.cuidador.Id)!;
        Assert.True(criador.EPrincipal);
        Assert.False(criador.EProvisorio);
        Assert.Null(this.ficha.ObterAssociacao(this.convidado.Id));
    }

    private Task Responder(bool aceitar, Guid conviteId)
    {
        return aceitar
            ? this.servico.AceitarAsync(this.convidado.Id, conviteId, CancellationToken.None)
            : this.servico.RecusarAsync(this.convidado.Id, conviteId, CancellationToken.None);
    }

    private Convite NovoConvite(string email, Papel papel)
    {
        var convite = new Convite(this.ficha.Id, email, papel, paraPrincipal: false, this.cuidador.Id, FichasDeTeste.Agora);
        this.repositorios.Convites.Add(convite);
        return convite;
    }

    private Convite ConviteParaPrincipal()
    {
        this.ficha.MarcarPrincipalProvisorio();
        var convite = new Convite(this.ficha.Id, "rui@exemplo.pt", Papel.Cuidador, paraPrincipal: true, this.cuidador.Id, FichasDeTeste.Agora);
        this.repositorios.Convites.Add(convite);
        return convite;
    }

    private Utilizador NovoUtilizador(string nome, string email)
    {
        var utilizador = new Utilizador(nome, email, "hash-ficticio", FichasDeTeste.Agora);
        this.repositorios.Utilizadores.Add(utilizador);
        return utilizador;
    }
}
