// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Fichas;

/// <summary>
/// Testes unitários da verificação do papel na ficha (Matriz de Permissões, RP-01 a RP-03 e RP-13).
/// </summary>
public class AutorizacaoFichaServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly AutorizacaoFichaService servico;
    private readonly Ficha ficha;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Guid familiar = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="AutorizacaoFichaServiceTestes"/> class.
    /// </summary>
    public AutorizacaoFichaServiceTestes()
    {
        this.servico = new AutorizacaoFichaService(this.repositorios);
        this.ficha = new Ficha("Utente Fictício", new DateOnly(1941, 5, 17), "+351 910 000 001", null, this.cuidador, Agora);
        this.ficha.AssociarCuidadorPrincipal(this.cuidador, provisorio: false, Agora);
        this.ficha.Associar(this.familiar, Papel.Familiar, Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-028: Cuidadores e Familiares associados consultam a ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorEFamiliarConsultam()
    {
        var doCuidador = await this.servico.ExigirConsultaAsync(this.cuidador, this.ficha.Id, CancellationToken.None);
        var doFamiliar = await this.servico.ExigirConsultaAsync(this.familiar, this.ficha.Id, CancellationToken.None);

        Assert.Same(this.ficha, doCuidador.Ficha);
        Assert.Equal(Papel.Cuidador, doCuidador.Associacao.Papel);
        Assert.Equal(Papel.Familiar, doFamiliar.Associacao.Papel);
    }

    /// <summary>
    /// RNF-005: quem não está associado, ou pede uma ficha inexistente, recebe a mesma resposta 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaoAssociadoEFichaInexistenteDao404()
    {
        var naoAssociado = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ExigirConsultaAsync(Guid.NewGuid(), this.ficha.Id, CancellationToken.None));
        var inexistente = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ExigirCuidadorAsync(this.cuidador, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(CodigosErro.FichaNaoEncontrada, naoAssociado.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, inexistente.Codigo);
        Assert.Equal(naoAssociado.Message, inexistente.Message);
        Assert.Null(await this.servico.ObterAcessoAsync(Guid.NewGuid(), this.ficha.Id, CancellationToken.None));
    }

    /// <summary>
    /// RF-048: o Familiar não faz operações de Cuidador (403).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoEscreve()
    {
        var erro = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ExigirCuidadorAsync(this.familiar, this.ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, erro.Codigo);
    }

    /// <summary>
    /// RP-13: numa ficha arquivada, as escritas que exigem ficha ativa são recusadas, mas o Cuidador continua a ser reconhecido.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaRecusaEscritasQueExigemFichaAtiva()
    {
        this.ficha.Arquivar(Agora);

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ExigirCuidadorDeFichaAtivaAsync(this.cuidador, this.ficha.Id, CancellationToken.None));
        var acesso = await this.servico.ExigirCuidadorAsync(this.cuidador, this.ficha.Id, CancellationToken.None);

        Assert.Equal(CodigosErro.FichaArquivada, erro.Codigo);
        Assert.True(acesso.Associacao.EPrincipal);
    }

    /// <summary>
    /// A ordem das verificações é 404, 403 e 409: o Familiar de uma ficha arquivada recebe 403.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OrdemDasVerificacoes()
    {
        this.ficha.Arquivar(Agora);

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ExigirCuidadorDeFichaAtivaAsync(this.familiar, this.ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, erro.Codigo);
    }
}
