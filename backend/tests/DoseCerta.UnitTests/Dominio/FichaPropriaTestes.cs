// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários da proteção do titular da ficha própria no agregado <see cref="Ficha"/> (F003, RF-033, RF-034).
/// </summary>
public class FichaPropriaTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Titular = Guid.NewGuid();

    /// <summary>
    /// Só o titular de uma ficha própria é titular; numa ficha de terceiro ninguém o é.
    /// </summary>
    [Fact]
    public void SoOTitularDaFichaPropriaETitular()
    {
        var propria = FichaPropriaComOutroCuidador(out var outroCuidador);
        var terceiro = NovaFicha(propria: false);
        terceiro.AssociarCuidadorPrincipal(Titular, provisorio: false, Agora);

        Assert.True(propria.ETitular(Titular));
        Assert.False(propria.ETitular(outroCuidador));
        Assert.False(propria.ETitular(Guid.NewGuid()));
        Assert.False(terceiro.ETitular(Titular));
    }

    /// <summary>
    /// RF-033 CA1 e CA2: despromover ou remover o titular é recusado, seja quem for que o peça (UC-15 E1).
    /// </summary>
    [Fact]
    public void TitularNaoPodeSerDespromovidoNemRemovido()
    {
        var ficha = FichaPropriaComOutroCuidador(out _);

        var erro = Assert.Throws<DominioException>(() => ficha.ExigirQueNaoETitular(Titular));

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.False(string.IsNullOrWhiteSpace(erro.Message));
        Assert.True(ficha.ObterAssociacao(Titular)!.EPrincipal);
    }

    /// <summary>
    /// RF-033: a proteção é só do titular; outro Cuidador da ficha própria pode ser despromovido ou removido.
    /// </summary>
    [Fact]
    public void OutroCuidadorDaFichaPropriaNaoEProtegido()
    {
        var ficha = FichaPropriaComOutroCuidador(out var outroCuidador);

        var erro = Record.Exception(() => ficha.ExigirQueNaoETitular(outroCuidador));

        Assert.Null(erro);
    }

    /// <summary>
    /// Numa ficha de terceiro o Cuidador principal não é protegido por esta regra (as regras dele são as do RF-133, da F004).
    /// </summary>
    [Fact]
    public void PrincipalDeFichaDeTerceiroNaoEProtegidoPorEstaRegra()
    {
        var ficha = NovaFicha(propria: false);
        ficha.AssociarCuidadorPrincipal(Titular, provisorio: false, Agora);

        Assert.Null(Record.Exception(() => ficha.ExigirQueNaoETitular(Titular)));
        Assert.Null(Record.Exception(ficha.ExigirPrincipalTransferivel));
    }

    /// <summary>
    /// RF-034 CA1: na ficha própria o papel de Cuidador principal não se transfere, nem a pedido do titular (UC-16 E1).
    /// </summary>
    [Fact]
    public void PrincipalDaFichaPropriaNaoSeTransfere()
    {
        var ficha = FichaPropriaComOutroCuidador(out _);

        var erro = Assert.Throws<DominioException>(ficha.ExigirPrincipalTransferivel);

        Assert.Equal(CodigosErro.CuidadorPrincipalProtegido, erro.Codigo);
        Assert.False(string.IsNullOrWhiteSpace(erro.Message));
    }

    private static Ficha FichaPropriaComOutroCuidador(out Guid outroCuidador)
    {
        var ficha = NovaFicha(propria: true);
        ficha.AssociarCuidadorPrincipal(Titular, provisorio: false, Agora);
        outroCuidador = Guid.NewGuid();
        ficha.Associar(outroCuidador, Papel.Cuidador, Agora);
        return ficha;
    }

    private static Ficha NovaFicha(bool propria)
    {
        return new Ficha("Pessoa Fictícia", new DateOnly(1980, 2, 29), "+351 910 000 003", propria ? Titular : null, Titular, Agora);
    }
}
