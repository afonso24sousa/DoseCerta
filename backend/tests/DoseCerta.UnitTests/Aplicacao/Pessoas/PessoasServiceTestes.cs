// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Fichas;
using DoseCerta.Application.Pessoas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Pessoas;

/// <summary>
/// Testes unitários do <see cref="PessoasService"/> (F004; OP-18).
/// </summary>
public class PessoasServiceTestes
{
    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly PessoasService servico;
    private readonly Utilizador principal;
    private readonly Utilizador familiar;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasServiceTestes"/> class.
    /// </summary>
    public PessoasServiceTestes()
    {
        this.servico = new PessoasService(this.repositorios, this.repositorios, new AutorizacaoFichaService(this.repositorios), this.unidadeDeTrabalho);
        this.principal = this.NovoUtilizador("Zé Principal", "ze@exemplo.pt");
        this.familiar = this.NovoUtilizador("Ana Familiar", "ana@exemplo.pt");
        this.ficha = FichasDeTeste.DeTerceiro(this.principal.Id);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, FichasDeTeste.Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-025, DP-03: o Cuidador e o Familiar veem as pessoas da ficha, com o nome, o e-mail e o papel, e o Cuidador principal primeiro.
    /// </summary>
    /// <param name="peloFamiliar">Indica se quem consulta é o Familiar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CuidadorEFamiliarVeemAsPessoasComOPrincipalPrimeiro(bool peloFamiliar)
    {
        var quem = peloFamiliar ? this.familiar.Id : this.principal.Id;

        var pessoas = await this.servico.ListarAsync(quem, this.ficha.Id, CancellationToken.None);

        Assert.Equal(2, pessoas.Count);
        Assert.Equal(new PessoaAssociada(this.principal.Id, "Zé Principal", "ze@exemplo.pt", Papel.Cuidador, true, false), pessoas[0]);
        Assert.Equal(new PessoaAssociada(this.familiar.Id, "Ana Familiar", "ana@exemplo.pt", Papel.Familiar, false, false), pessoas[1]);
    }

    /// <summary>
    /// RNF-005, RP-02: quem não está associado, ou pede uma ficha inexistente, recebe 404 sem ver as pessoas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaoAssociadoEFichaInexistenteDao404()
    {
        var naoAssociado = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ListarAsync(Guid.NewGuid(), this.ficha.Id, CancellationToken.None));
        var inexistente = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ListarAsync(this.principal.Id, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(CodigosErro.FichaNaoEncontrada, naoAssociado.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, inexistente.Codigo);
    }

    /// <summary>
    /// RF-049 e RF-130: o Cuidador promove o Familiar e depois despromove-o; cada pedido devolve a pessoa com o nome, o e-mail
    /// e o novo papel, numa transação confirmada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PromoverEDespromoverDevolvemAPessoaComONovoPapel()
    {
        var promovido = await this.servico.AlterarPapelAsync(this.principal.Id, this.ficha.Id, this.familiar.Id, Papel.Cuidador, CancellationToken.None);
        var despromovido = await this.servico.AlterarPapelAsync(this.principal.Id, this.ficha.Id, this.familiar.Id, Papel.Familiar, CancellationToken.None);

        Assert.Equal(new PessoaAssociada(this.familiar.Id, "Ana Familiar", "ana@exemplo.pt", Papel.Cuidador, false, false), promovido);
        Assert.Equal(Papel.Familiar, despromovido.Papel);
        Assert.Equal(Papel.Familiar, this.ficha.ObterAssociacao(this.familiar.Id)!.Papel);
        Assert.Equal(2, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RNF-028: o papel é obrigatório e só pode ser Cuidador ou Familiar, incluindo quando chega como número; nada é gravado.
    /// </summary>
    /// <param name="valor">Valor do papel, ou <see langword="null"/> se faltar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData(99)]
    public async Task PapelEmFaltaOuInvalidoDa400(int? valor)
    {
        var papel = (Papel?)valor;

        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.servico.AlterarPapelAsync(this.principal.Id, this.ficha.Id, this.familiar.Id, papel, CancellationToken.None));

        Assert.Equal("papel", Assert.Single(erro.Campos).Campo);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RNF-004, RF-048: o Familiar não altera papéis (403), e numa ficha arquivada a operação é recusada com 409 (RP-13).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarEFichaArquivadaSaoRecusados()
    {
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.AlterarPapelAsync(this.familiar.Id, this.ficha.Id, this.familiar.Id, Papel.Cuidador, CancellationToken.None));
        this.ficha.Arquivar(FichasDeTeste.Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.AlterarPapelAsync(this.principal.Id, this.ficha.Id, this.familiar.Id, Papel.Cuidador, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Equal(Papel.Familiar, this.ficha.ObterAssociacao(this.familiar.Id)!.Papel);
    }

    private Utilizador NovoUtilizador(string nome, string email)
    {
        var utilizador = new Utilizador(nome, email, "hash-ficticio", FichasDeTeste.Agora);
        this.repositorios.Utilizadores.Add(utilizador);
        return utilizador;
    }
}
