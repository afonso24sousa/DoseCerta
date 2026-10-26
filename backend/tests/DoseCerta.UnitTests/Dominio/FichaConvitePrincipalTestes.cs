// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários do convite para Cuidador principal no agregado <see cref="Ficha"/> (F002, F004, RF-025 a RF-027, RF-034; OP-19, OP-21, OP-22).
/// </summary>
public class FichaConvitePrincipalTestes
{
    private static readonly Guid Criador = Guid.NewGuid();
    private static readonly Guid Convidado = Guid.NewGuid();
    private static readonly DateTime Aceite = FichasDeTeste.Agora.AddDays(1);

    /// <summary>
    /// RF-026 CA1: aceitar o convite deixa o convidado como Cuidador principal, não provisório, e o criador provisório deixa de estar associado.
    /// </summary>
    [Fact]
    public void AceitarDeixaOConvidadoComoPrincipalERetiraOCriador()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador, provisorio: true);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        var novo = ficha.AceitarPrincipalConvidado(Convidado, Aceite);

        Assert.Equal(Convidado, novo.UtilizadorId);
        Assert.Equal(Papel.Cuidador, novo.Papel);
        Assert.True(novo.EPrincipal);
        Assert.False(novo.EProvisorio);
        Assert.Equal(Aceite, novo.CriadaEmUtc);
        Assert.Null(ficha.ObterAssociacao(Criador));
        Assert.NotNull(ficha.ObterAssociacao(familiar));
        Assert.Single(ficha.Associacoes, a => a.EPrincipal);
    }

    /// <summary>
    /// RF-027 CA1: recusar o convite mantém o criador como Cuidador principal, que deixa de ser provisório e pode convidar outra pessoa.
    /// </summary>
    [Fact]
    public void RecusarMantemOCriadorComoPrincipalEPermiteNovoConvite()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador, provisorio: true);

        ficha.RecusarPrincipalConvidado();

        var criador = ficha.ObterAssociacao(Criador)!;
        Assert.True(criador.EPrincipal);
        Assert.False(criador.EProvisorio);
        ficha.MarcarPrincipalProvisorio();
        Assert.True(criador.EProvisorio);
    }

    /// <summary>
    /// D2a, RF-025: o convite para Cuidador principal feito na OP-19 deixa provisório o Cuidador principal atual.
    /// </summary>
    [Fact]
    public void ConviteParaPrincipalDeixaOPrincipalAtualProvisorio()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador);
        FichasDeTeste.Associar(ficha, Papel.Cuidador);

        ficha.MarcarPrincipalProvisorio();

        var principal = ficha.ObterAssociacao(Criador)!;
        Assert.True(principal.EPrincipal);
        Assert.True(principal.EProvisorio);
    }

    /// <summary>
    /// D2b: com um convite para Cuidador principal pendente (principal provisório), um segundo é recusado com 409.
    /// </summary>
    [Fact]
    public void SegundoConviteParaPrincipalERecusado()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador, provisorio: true);

        var erro = Assert.Throws<DominioException>(ficha.MarcarPrincipalProvisorio);

        Assert.Equal(CodigosErro.ConvitePendenteExiste, erro.Codigo);
        Assert.Contains("convite para Cuidador principal pendente", erro.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// RF-034, UC-13 E6: na ficha própria não há convite para Cuidador principal.
    /// </summary>
    [Fact]
    public void FichaPropriaNaoTemConviteParaPrincipal()
    {
        var ficha = FichasDeTeste.Propria(Criador);

        var erro = Assert.Throws<DominioException>(ficha.MarcarPrincipalProvisorio);

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.False(ficha.ObterAssociacao(Criador)!.EProvisorio);
    }

    /// <summary>
    /// D4: se o convidado já estiver associado à ficha, a aceitação é recusada com 409 e nada muda.
    /// </summary>
    /// <param name="convidadoEOCriador">Indica se o convidado é o próprio criador provisório (convite para o seu e-mail na OP-09).</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConvidadoJaAssociadoNaoAceita(bool convidadoEOCriador)
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador, provisorio: true);
        var convidado = convidadoEOCriador ? Criador : FichasDeTeste.Associar(ficha, Papel.Familiar);

        var erro = Assert.Throws<DominioException>(() => ficha.AceitarPrincipalConvidado(convidado, Aceite));

        Assert.Equal(CodigosErro.PessoaJaAssociada, erro.Codigo);
        var criador = ficha.ObterAssociacao(Criador)!;
        Assert.True(criador.EPrincipal);
        Assert.True(criador.EProvisorio);
    }

    /// <summary>
    /// D3: depois de uma transferência com o convite pendente, a aceitação retira o novo principal provisório e mantém o criador como Cuidador.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AceitarDepoisDeUmaTransferenciaRetiraOPrincipalProvisorio()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador, provisorio: true);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);
        await ficha.TransferirPrincipalAsync(cuidador, () => Task.CompletedTask);

        ficha.AceitarPrincipalConvidado(Convidado, Aceite);

        Assert.Null(ficha.ObterAssociacao(cuidador));
        var criador = ficha.ObterAssociacao(Criador)!;
        Assert.Equal(Papel.Cuidador, criador.Papel);
        Assert.False(criador.EPrincipal);
        Assert.True(ficha.ObterAssociacao(Convidado)!.EPrincipal);
    }

    /// <summary>
    /// Sem Cuidador principal provisório não existe convite para Cuidador principal pendente: responder é um defeito do chamador.
    /// </summary>
    [Fact]
    public void ResponderSemPrincipalProvisorioEUmDefeito()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador);

        Assert.Throws<InvalidOperationException>(() => ficha.AceitarPrincipalConvidado(Convidado, Aceite));
        Assert.Throws<InvalidOperationException>(ficha.RecusarPrincipalConvidado);
        Assert.True(ficha.ObterAssociacao(Criador)!.EPrincipal);
    }

    /// <summary>
    /// OP-19, OP-21: <see cref="Ficha.ExigirQueNaoEstaAssociado"/> recusa uma pessoa associada com qualquer papel e aceita uma não associada.
    /// </summary>
    [Fact]
    public void SoAPessoaNaoAssociadaPodeSerAssociada()
    {
        var ficha = FichasDeTeste.DeTerceiro(Criador);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        Assert.Equal(CodigosErro.PessoaJaAssociada, Assert.Throws<DominioException>(() => ficha.ExigirQueNaoEstaAssociado(Criador)).Codigo);
        Assert.Equal(CodigosErro.PessoaJaAssociada, Assert.Throws<DominioException>(() => ficha.ExigirQueNaoEstaAssociado(familiar)).Codigo);
        Assert.Null(Record.Exception(() => ficha.ExigirQueNaoEstaAssociado(Convidado)));
    }
}
