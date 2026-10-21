// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários da promoção e da despromoção no agregado <see cref="Ficha"/> (F004, RF-049, RF-130, RF-133, RF-134; OP-23, OP-24).
/// </summary>
public class FichaPapeisTestes
{
    private static readonly Guid Principal = Guid.NewGuid();

    /// <summary>
    /// RF-049 CA1: o Familiar promovido passa a Cuidador, com permissão de escrita, sem ser Cuidador principal.
    /// </summary>
    [Fact]
    public void PromoverFamiliarDaPermissoesDeCuidador()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        var associacao = ficha.AlterarPapel(familiar, Papel.Cuidador);

        Assert.Equal(Papel.Cuidador, associacao.Papel);
        Assert.True(associacao.PodeEscrever());
        Assert.False(associacao.EPrincipal);
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
    }

    /// <summary>
    /// RF-130 CA1: um Cuidador que não é o principal nem o titular passa a Familiar e perde a permissão de escrita.
    /// </summary>
    [Fact]
    public void DespromoverCuidadorDeixaSoAsPermissoesDeFamiliar()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var associacao = ficha.AlterarPapel(cuidador, Papel.Familiar);

        Assert.Equal(Papel.Familiar, associacao.Papel);
        Assert.False(associacao.PodeEscrever());
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
    }

    /// <summary>
    /// UC-15 E5: a pessoa visada tem de estar associada à ficha, para qualquer papel pretendido.
    /// </summary>
    /// <param name="papel">Papel pretendido.</param>
    [Theory]
    [InlineData(Papel.Cuidador)]
    [InlineData(Papel.Familiar)]
    public void PessoaNaoAssociadaNaoEEncontrada(Papel papel)
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);

        var erro = Assert.Throws<DominioException>(() => ficha.AlterarPapel(Guid.NewGuid(), papel));

        Assert.Equal(CodigosErro.PessoaNaoEncontrada, erro.Codigo);
    }

    /// <summary>
    /// RF-134 CA1: numa ficha com um único Cuidador e dois Familiares, despromover esse Cuidador é recusado e nada muda.
    /// </summary>
    [Fact]
    public void UnicoCuidadorNaoEDespromovido()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        FichasDeTeste.Associar(ficha, Papel.Familiar);
        FichasDeTeste.Associar(ficha, Papel.Familiar);

        var erro = Assert.Throws<DominioException>(() => ficha.AlterarPapel(Principal, Papel.Familiar));

        Assert.Equal(CodigosErro.FichaSemCuidador, erro.Codigo);
        Assert.Contains("promova primeiro um Familiar ou convide um novo Cuidador", erro.Message, StringComparison.Ordinal);
        Assert.Equal(Papel.Cuidador, ficha.ObterAssociacao(Principal)!.Papel);
    }

    /// <summary>
    /// RF-133: com outro Cuidador na ficha, o Cuidador principal só é despromovido depois de transferir o papel (OP-26), mesmo a pedido próprio.
    /// </summary>
    [Fact]
    public void PrincipalNaoEDespromovidoSemSubstituto()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var erro = Assert.Throws<DominioException>(() => ficha.AlterarPapel(Principal, Papel.Familiar));

        Assert.Equal(CodigosErro.CuidadorPrincipalSemSubstituto, erro.Codigo);
        Assert.Contains("/cuidador-principal", erro.Message, StringComparison.Ordinal);
        var principal = ficha.ObterAssociacao(Principal)!;
        Assert.Equal(Papel.Cuidador, principal.Papel);
        Assert.True(principal.EPrincipal);
    }

    /// <summary>
    /// RF-033: o titular da ficha própria não é despromovido; a proteção vem antes das outras regras, com ou sem outro Cuidador.
    /// </summary>
    /// <param name="comOutroCuidador">Indica se a ficha tem outro Cuidador.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TitularNaoEDespromovido(bool comOutroCuidador)
    {
        var ficha = FichasDeTeste.Propria(Principal);
        if (comOutroCuidador)
        {
            FichasDeTeste.Associar(ficha, Papel.Cuidador);
        }

        var erro = Assert.Throws<DominioException>(() => ficha.AlterarPapel(Principal, Papel.Familiar));

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.Equal(Papel.Cuidador, ficha.ObterAssociacao(Principal)!.Papel);
    }

    /// <summary>
    /// RF-032, RF-130: na ficha própria, outro Cuidador pode ser despromovido como nas restantes fichas.
    /// </summary>
    [Fact]
    public void OutroCuidadorDaFichaPropriaEDespromovido()
    {
        var ficha = FichasDeTeste.Propria(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var associacao = ficha.AlterarPapel(cuidador, Papel.Familiar);

        Assert.Equal(Papel.Familiar, associacao.Papel);
    }

    /// <summary>
    /// D7: um pedido com o papel atual não altera nada e não é recusado, incluindo sobre o Cuidador principal e o titular.
    /// </summary>
    /// <param name="propria">Indica se a ficha é própria.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PedidoComOPapelAtualNaoAlteraNada(bool propria)
    {
        var ficha = propria ? FichasDeTeste.Propria(Principal) : FichasDeTeste.DeTerceiro(Principal);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        var principal = ficha.AlterarPapel(Principal, Papel.Cuidador);
        var outro = ficha.AlterarPapel(familiar, Papel.Familiar);

        Assert.Equal(Papel.Cuidador, principal.Papel);
        Assert.True(principal.EPrincipal);
        Assert.Equal(Papel.Familiar, outro.Papel);
    }
}
