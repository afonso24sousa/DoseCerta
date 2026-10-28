// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários de <see cref="PoliticaTolerancia"/> (RF-065 a RF-068).
/// </summary>
public class PoliticaToleranciaTestes
{
    private static readonly DateTime Prevista = new(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// RF-066: as tolerâncias são exatamente Leve 2 h, Moderado 1 h e Grave 30 min.
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    /// <param name="minutos">Tolerância esperada, em minutos.</param>
    [Theory]
    [InlineData(GrauImportancia.Leve, 120)]
    [InlineData(GrauImportancia.Moderado, 60)]
    [InlineData(GrauImportancia.Grave, 30)]
    public void ToleranciaDeCadaGrau(GrauImportancia grau, int minutos)
    {
        Assert.Equal(TimeSpan.FromMinutes(minutos), PoliticaTolerancia.ToleranciaDoGrau(grau));
        Assert.Equal(Prevista.AddMinutes(minutos), PoliticaTolerancia.FimTolerancia(Prevista, grau));
    }

    /// <summary>
    /// Um grau fora do enumerado é recusado.
    /// </summary>
    [Fact]
    public void GrauDesconhecidoERecusado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoliticaTolerancia.ToleranciaDoGrau((GrauImportancia)99));
    }

    /// <summary>
    /// RF-068: a toma passa a não confirmada 24 horas depois da hora prevista; os Familiares de uma toma Grave são avisados aos 60 minutos.
    /// </summary>
    [Fact]
    public void LimitesDeTempoDaToma()
    {
        Assert.Equal(Prevista.AddHours(24), PoliticaTolerancia.InstanteNaoConfirmada(Prevista));
        Assert.Equal(Prevista.AddMinutes(60), PoliticaTolerancia.InstanteFamiliaresGrave(Prevista));
    }
}
