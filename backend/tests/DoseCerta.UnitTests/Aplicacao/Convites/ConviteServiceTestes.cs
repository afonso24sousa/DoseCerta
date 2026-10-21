// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Convites;

/// <summary>
/// Testes unitários da criação de convites no <see cref="ConviteService"/> (F004; OP-19; RF-025, RF-040, RF-043).
/// </summary>
public class ConviteServiceTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora.AddHours(1);

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly ConviteService servico;
    private readonly Utilizador cuidador;
    private readonly Utilizador familiar;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConviteServiceTestes"/> class.
    /// </summary>
    public ConviteServiceTestes()
    {
        this.servico = new ConviteService(
            this.repositorios,
            this.repositorios,
            this.repositorios,
            new AutorizacaoFichaService(this.repositorios),
            this.unidadeDeTrabalho,
            new RelogioFixo(Agora));
        this.cuidador = this.NovoUtilizador("Zé Cuidador", "ze@exemplo.pt");
        this.familiar = this.NovoUtilizador("Ana Familiar", "ana@exemplo.pt");
        this.ficha = FichasDeTeste.DeTerceiro(this.cuidador.Id);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, FichasDeTeste.Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-040 CA1: o Cuidador convida um e-mail como Familiar; o convite fica pendente, com o papel e quem convidou, e não associa ninguém.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteFicaPendenteSemAssociar()
    {
        var resumo = await this.Convidar(" rui@exemplo.pt ", Papel.Familiar);

        var convite = Assert.Single(this.repositorios.Convites);
        Assert.Equal("rui@exemplo.pt", convite.EmailConvidado);
        Assert.Equal(this.ficha.Id, convite.FichaId);
        Assert.Equal(Papel.Familiar, convite.PapelOferecido);
        Assert.False(convite.ParaPrincipal);
        Assert.Equal(this.cuidador.Id, convite.ConvidadoPorId);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.Equal(Agora, convite.CriadoEmUtc);
        Assert.Equal(
            new ConviteResumo(convite.Id, this.ficha.Id, this.ficha.NomeCompleto, Papel.Familiar, false, "Zé Cuidador", EstadoConvite.Pendente, Agora),
            resumo);
        Assert.Equal(2, this.ficha.Associacoes.Count);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-040 CA2, RNF-028: sem papel, com e-mail inválido ou com <c>paraPrincipal=true</c> e papel Familiar, o convite não é criado.
    /// </summary>
    /// <param name="email">E-mail submetido.</param>
    /// <param name="papel">Papel submetido, ou <see langword="null"/>.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal.</param>
    /// <param name="campoEmErro">Campo indicado no erro.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("rui@exemplo.pt", null, false, "papel")]
    [InlineData("rui@exemplo.pt", 99, false, "papel")]
    [InlineData("nao-e-email", 1, false, "email")]
    [InlineData(null, 1, false, "email")]
    [InlineData("rui@exemplo.pt", 2, true, "papel")]
    public async Task DadosInvalidosNaoCriamConvite(string? email, int? papel, bool paraPrincipal, string campoEmErro)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => this.Convidar(email, (Papel?)papel, paraPrincipal));

        Assert.Equal(campoEmErro, Assert.Single(erro.Campos).Campo);
        Assert.Empty(this.repositorios.Convites);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-043 CA1: com um convite pendente para o mesmo e-mail e a mesma ficha, sem distinguir maiúsculas, o novo convite é recusado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitePendenteDuplicadoERecusado()
    {
        await this.Convidar("rui@exemplo.pt", Papel.Familiar);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.Convidar("Rui@Exemplo.PT", Papel.Cuidador));

        Assert.Equal(CodigosErro.ConvitePendenteExiste, erro.Codigo);
        Assert.Single(this.repositorios.Convites);
    }

    /// <summary>
    /// RF-043 CA3: um convite pendente noutra ficha não impede o convite para esta.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitePendenteNoutraFichaNaoImpede()
    {
        this.repositorios.Convites.Add(new Convite(Guid.NewGuid(), "rui@exemplo.pt", Papel.Familiar, false, Guid.NewGuid(), FichasDeTeste.Agora));

        await this.Convidar("rui@exemplo.pt", Papel.Familiar);

        Assert.Equal(2, this.repositorios.Convites.Count);
    }

    /// <summary>
    /// OP-19: o e-mail de uma pessoa já associada à ficha, incluindo o de quem convida, é recusado com 409 <c>PESSOA_JA_ASSOCIADA</c>.
    /// </summary>
    /// <param name="email">E-mail convidado.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("ANA@exemplo.pt")]
    [InlineData("ze@exemplo.pt")]
    public async Task PessoaJaAssociadaNaoEConvidada(string email)
    {
        var erro = await Assert.ThrowsAsync<DominioException>(() => this.Convidar(email, Papel.Cuidador));

        Assert.Equal(CodigosErro.PessoaJaAssociada, erro.Codigo);
        Assert.Empty(this.repositorios.Convites);
    }

    /// <summary>
    /// RF-025, D2a: o convite para Cuidador principal deixa o Cuidador principal atual provisório; D2b: um segundo convite para
    /// Cuidador principal, para outro e-mail, é recusado com 409 <c>CONVITE_PENDENTE_EXISTE</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteParaPrincipalDeixaOPrincipalProvisorioEImpedeOutro()
    {
        var resumo = await this.Convidar("rui@exemplo.pt", Papel.Cuidador, paraPrincipal: true);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.Convidar("rita@exemplo.pt", Papel.Cuidador, paraPrincipal: true));

        Assert.True(resumo.ParaPrincipal);
        Assert.True(this.ficha.ObterAssociacao(this.cuidador.Id)!.EProvisorio);
        Assert.Equal(CodigosErro.ConvitePendenteExiste, erro.Codigo);
        Assert.Contains("Cuidador principal", erro.Message, StringComparison.Ordinal);
        Assert.Single(this.repositorios.Convites);
    }

    /// <summary>
    /// OP-09, RF-025: o convite para Cuidador principal da criação da ficha é registado pendente, com o papel Cuidador e a marca de
    /// Cuidador principal, sem alterar as associações (o criador já foi associado como provisório pela ficha).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteParaPrincipalDaCriacaoDaFichaFicaPendente()
    {
        var nova = FichasDeTeste.DeTerceiro(this.cuidador.Id, provisorio: true);

        var convite = await this.servico.ConvidarParaPrincipalAsync(nova, this.cuidador.Id, "rui@exemplo.pt", Agora, CancellationToken.None);

        Assert.Same(convite, Assert.Single(this.repositorios.Convites));
        Assert.Equal(nova.Id, convite.FichaId);
        Assert.Equal("rui@exemplo.pt", convite.EmailConvidado);
        Assert.Equal(Papel.Cuidador, convite.PapelOferecido);
        Assert.True(convite.ParaPrincipal);
        Assert.Equal(this.cuidador.Id, convite.ConvidadoPorId);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.Equal(Agora, convite.CriadoEmUtc);
        Assert.Single(nova.Associacoes);
    }

    /// <summary>
    /// RF-034, UC-13 E6: na ficha própria o convite para Cuidador principal é recusado com 403; os outros convites são aceites (RF-032).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaFichaPropriaSoOConviteParaPrincipalERecusado()
    {
        var propria = FichasDeTeste.Propria(this.cuidador.Id);
        this.repositorios.Fichas.Add(propria);

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ConvidarAsync(this.cuidador.Id, propria.Id, "rui@exemplo.pt", Papel.Cuidador, true, CancellationToken.None));
        await this.servico.ConvidarAsync(this.cuidador.Id, propria.Id, "rui@exemplo.pt", Papel.Cuidador, false, CancellationToken.None);

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.False(propria.ObterAssociacao(this.cuidador.Id)!.EProvisorio);
        Assert.False(Assert.Single(this.repositorios.Convites).ParaPrincipal);
    }

    /// <summary>
    /// RF-048 CA2, RP-13: o Familiar não convida (403) e, numa ficha arquivada, o convite é recusado (409).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarEFichaArquivadaNaoConvidam()
    {
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ConvidarAsync(this.familiar.Id, this.ficha.Id, "rui@exemplo.pt", Papel.Familiar, false, CancellationToken.None));
        this.ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(() => this.Convidar("rui@exemplo.pt", Papel.Familiar));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Empty(this.repositorios.Convites);
    }

    /// <summary>
    /// RF-042 CA1, RF-044: o Utilizador vê só os convites pendentes dirigidos ao seu e-mail, sem distinguir maiúsculas, com a ficha
    /// e quem convidou, do mais recente para o mais antigo; os convites respondidos e os de outros e-mails não aparecem.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarRecebidosDevolveSoOsPendentesDoSeuEmail()
    {
        var convidado = this.NovoUtilizador("Rui Convidado", "rui@exemplo.pt");
        var outraFicha = FichasDeTeste.DeTerceiro(this.cuidador.Id);
        this.repositorios.Fichas.Add(outraFicha);
        var antigo = new Convite(this.ficha.Id, "RUI@exemplo.pt", Papel.Familiar, false, this.cuidador.Id, Agora.AddDays(-2));
        var recente = new Convite(outraFicha.Id, "rui@exemplo.pt", Papel.Cuidador, false, this.cuidador.Id, Agora);
        var respondido = new Convite(this.ficha.Id, "rui@exemplo.pt", Papel.Familiar, false, this.cuidador.Id, Agora.AddDays(-5));
        respondido.Recusar(convidado.Id, Agora.AddDays(-4));
        var outroEmail = new Convite(this.ficha.Id, "rita@exemplo.pt", Papel.Familiar, false, this.cuidador.Id, Agora);
        this.repositorios.Convites.AddRange(new[] { antigo, recente, respondido, outroEmail });

        var recebidos = await this.servico.ListarRecebidosAsync(convidado.Id, CancellationToken.None);

        Assert.Equal(new[] { recente.Id, antigo.Id }, recebidos.Select(r => r.Id));
        Assert.Equal(
            new ConviteResumo(antigo.Id, this.ficha.Id, this.ficha.NomeCompleto, Papel.Familiar, false, "Zé Cuidador", EstadoConvite.Pendente, Agora.AddDays(-2)),
            recebidos[1]);
    }

    /// <summary>
    /// Se a conta do token já não existir, a listagem é recusada com 401, como na consulta da conta (OP-04).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarRecebidosSemContaDa401()
    {
        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ListarRecebidosAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(CodigosErro.NaoAutenticado, erro.Codigo);
    }

    private Task<ConviteResumo> Convidar(string? email, Papel? papel, bool paraPrincipal = false)
    {
        return this.servico.ConvidarAsync(this.cuidador.Id, this.ficha.Id, email, papel, paraPrincipal, CancellationToken.None);
    }

    private Utilizador NovoUtilizador(string nome, string email)
    {
        var utilizador = new Utilizador(nome, email, "hash-ficticio", FichasDeTeste.Agora);
        this.repositorios.Utilizadores.Add(utilizador);
        return utilizador;
    }
}
