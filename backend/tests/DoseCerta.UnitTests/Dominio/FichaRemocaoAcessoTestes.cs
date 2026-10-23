// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários da remoção de acesso no agregado <see cref="Ficha"/> (F004, RF-131, RF-133, RF-134; OP-25).
/// </summary>
public class FichaRemocaoAcessoTestes
{
    private static readonly Guid Principal = Guid.NewGuid();

    /// <summary>
    /// RF-131 CA1: o Familiar removido deixa de estar associado à ficha; as outras pessoas mantêm-se.
    /// </summary>
    [Fact]
    public void FamiliarRemovidoDeixaDeEstarAssociado()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);
        var outroFamiliar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        ficha.RemoverAcesso(familiar);

        Assert.Null(ficha.ObterAssociacao(familiar));
        Assert.NotNull(ficha.ObterAssociacao(outroFamiliar));
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
        Assert.Equal(2, ficha.Associacoes.Count);
    }

    /// <summary>
    /// RF-131: um Cuidador que não é o principal pode ser removido, também por sua iniciativa (Matriz de Permissões, secção 6).
    /// </summary>
    [Fact]
    public void CuidadorQueNaoEOPrincipalERemovido()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        var cuidador = FichasDeTeste.Associar(ficha, Papel.Cuidador);

        ficha.RemoverAcesso(cuidador);

        Assert.Null(ficha.ObterAssociacao(cuidador));
        Assert.Single(ficha.Associacoes);
    }

    /// <summary>
    /// UC-15 E5: a pessoa a remover tem de estar associada à ficha.
    /// </summary>
    [Fact]
    public void PessoaNaoAssociadaNaoEEncontrada()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);

        var erro = Assert.Throws<DominioException>(() => ficha.RemoverAcesso(Guid.NewGuid()));

        Assert.Equal(CodigosErro.PessoaNaoEncontrada, erro.Codigo);
        Assert.Single(ficha.Associacoes);
    }

    /// <summary>
    /// RF-133 CA1 e RF-134 CA1: o único Cuidador, que é o principal, não é removido, mesmo a pedido próprio,
    /// e é indicado que deve primeiro promover um Familiar ou convidar um novo Cuidador.
    /// </summary>
    /// <param name="familiares">Número de Familiares da ficha.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void UnicoCuidadorNaoERemovido(int familiares)
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        for (var i = 0; i < familiares; i++)
        {
            FichasDeTeste.Associar(ficha, Papel.Familiar);
        }

        var erro = Assert.Throws<DominioException>(() => ficha.RemoverAcesso(Principal));

        Assert.Equal(CodigosErro.FichaSemCuidador, erro.Codigo);
        Assert.Contains("promova primeiro um Familiar ou convide um novo Cuidador", erro.Message, StringComparison.Ordinal);
        Assert.NotNull(ficha.ObterAssociacao(Principal));
        Assert.Equal(familiares + 1, ficha.Associacoes.Count);
    }

    /// <summary>
    /// RF-133, R-06: com outro Cuidador na ficha, o Cuidador principal só é removido depois de transferir o papel (OP-26).
    /// </summary>
    [Fact]
    public void PrincipalNaoERemovidoSemSubstituto()
    {
        var ficha = FichasDeTeste.DeTerceiro(Principal);
        FichasDeTeste.Associar(ficha, Papel.Cuidador);

        var erro = Assert.Throws<DominioException>(() => ficha.RemoverAcesso(Principal));

        Assert.Equal(CodigosErro.CuidadorPrincipalSemSubstituto, erro.Codigo);
        Assert.True(ficha.ObterAssociacao(Principal)!.EPrincipal);
    }

    /// <summary>
    /// RF-033 CA2: o titular da ficha própria não é removido; a proteção vem antes das outras regras, com ou sem outro Cuidador.
    /// </summary>
    /// <param name="comOutroCuidador">Indica se a ficha tem outro Cuidador.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TitularNaoERemovido(bool comOutroCuidador)
    {
        var ficha = FichasDeTeste.Propria(Principal);
        if (comOutroCuidador)
        {
            FichasDeTeste.Associar(ficha, Papel.Cuidador);
        }

        var erro = Assert.Throws<DominioException>(() => ficha.RemoverAcesso(Principal));

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.NotNull(ficha.ObterAssociacao(Principal));
    }

    /// <summary>
    /// RF-032, RF-131: na ficha própria, as outras pessoas são removidas como nas restantes fichas.
    /// </summary>
    [Fact]
    public void OutraPessoaDaFichaPropriaERemovida()
    {
        var ficha = FichasDeTeste.Propria(Principal);
        var familiar = FichasDeTeste.Associar(ficha, Papel.Familiar);

        ficha.RemoverAcesso(familiar);

        Assert.Null(ficha.ObterAssociacao(familiar));
    }
}
