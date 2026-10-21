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
    private readonly PessoasService servico;
    private readonly Utilizador principal;
    private readonly Utilizador familiar;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasServiceTestes"/> class.
    /// </summary>
    public PessoasServiceTestes()
    {
        this.servico = new PessoasService(this.repositorios, new AutorizacaoFichaService(this.repositorios));
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

    private Utilizador NovoUtilizador(string nome, string email)
    {
        var utilizador = new Utilizador(nome, email, "hash-ficticio", FichasDeTeste.Agora);
        this.repositorios.Utilizadores.Add(utilizador);
        return utilizador;
    }
}
