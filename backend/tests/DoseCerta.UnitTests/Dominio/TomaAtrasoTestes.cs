// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários das regras de tolerância das notificações de atraso em <see cref="Toma"/> (RF-110 a RF-113; TR-05 e TR-07
/// do diagrama de estados da toma). A toma está prevista para 7 de outubro de 2026, 10:00 de Lisboa (09:00 UTC).
/// </summary>
public class TomaAtrasoTestes
{
    private static readonly DateTime Prevista = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CuidadorId = Guid.NewGuid();

    /// <summary>
    /// RF-110 a RF-112: os Cuidadores só são avisados estritamente depois do fim da tolerância exata do grau (Leve 2 h,
    /// Moderado 1 h, Grave 30 min); no último instante da tolerância ainda não.
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    /// <param name="minutos">Tolerância do grau, em minutos.</param>
    [Theory]
    [InlineData(GrauImportancia.Leve, 120)]
    [InlineData(GrauImportancia.Moderado, 60)]
    [InlineData(GrauImportancia.Grave, 30)]
    public void CuidadoresSoDepoisDaToleranciaExataDoGrau(GrauImportancia grau, int minutos)
    {
        var toma = Nova(grau);

        Assert.False(toma.AtrasoCuidadoresDevido(Prevista.AddMinutes(minutos).AddSeconds(-1)));
        Assert.False(toma.AtrasoCuidadoresDevido(Prevista.AddMinutes(minutos)));
        Assert.True(toma.AtrasoCuidadoresDevido(Prevista.AddMinutes(minutos).AddSeconds(1)));
    }

    /// <summary>
    /// RF-113: no grau Grave os Familiares são avisados estritamente depois dos 60 minutos, ou seja, 30 minutos depois do fim
    /// da tolerância dos Cuidadores.
    /// </summary>
    [Fact]
    public void FamiliaresDoGraveSoDepoisDeSessentaMinutos()
    {
        var toma = Nova(GrauImportancia.Grave);

        Assert.Equal(Prevista.AddMinutes(60), toma.InstanteFamiliaresGrave());
        Assert.Equal(toma.FimTolerancia().AddMinutes(30), toma.InstanteFamiliaresGrave());
        Assert.False(toma.AtrasoFamiliaresDevido(Prevista.AddMinutes(45)));
        Assert.False(toma.AtrasoFamiliaresDevido(Prevista.AddMinutes(60)));
        Assert.True(toma.AtrasoFamiliaresDevido(Prevista.AddMinutes(60).AddSeconds(1)));
    }

    /// <summary>
    /// RF-110 CA3 e RF-111 CA3: nos graus Leve e Moderado os Familiares nunca são avisados, a qualquer hora.
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    [Theory]
    [InlineData(GrauImportancia.Leve)]
    [InlineData(GrauImportancia.Moderado)]
    public void FamiliaresNuncaNosGrausLeveEModerado(GrauImportancia grau)
    {
        var toma = Nova(grau);

        Assert.False(toma.AtrasoFamiliaresDevido(Prevista.AddHours(3)));
        toma.MarcarFamiliaresNotificados(Prevista.AddHours(3));
        Assert.Null(toma.FamiliaresNotificadosEmUtc);
    }

    /// <summary>
    /// TR-05 e RNF-043: marcar o aviso aos Cuidadores não muda o estado da toma (continua <c>Pendente</c>, RF-066) e o aviso
    /// deixa de ser devido; marcar antes do tempo não faz nada.
    /// </summary>
    [Fact]
    public void MarcarCuidadoresMantemPendenteENaoRepete()
    {
        var toma = Nova(GrauImportancia.Moderado);
        var antes = Prevista.AddMinutes(30);
        var depois = Prevista.AddMinutes(61);

        toma.MarcarCuidadoresNotificados(antes);
        Assert.Null(toma.CuidadoresNotificadosEmUtc);

        toma.MarcarCuidadoresNotificados(depois);

        Assert.Equal(depois, toma.CuidadoresNotificadosEmUtc);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.False(toma.AtrasoCuidadoresDevido(depois.AddMinutes(10)));
        toma.MarcarCuidadoresNotificados(depois.AddMinutes(10));
        Assert.Equal(depois, toma.CuidadoresNotificadosEmUtc);
    }

    /// <summary>
    /// TR-07: marcar o aviso aos Familiares de uma toma Grave mantém-na <c>Pendente</c> e não se repete.
    /// </summary>
    [Fact]
    public void MarcarFamiliaresMantemPendenteENaoRepete()
    {
        var toma = Nova(GrauImportancia.Grave);
        var depois = Prevista.AddMinutes(61);

        toma.MarcarFamiliaresNotificados(depois);

        Assert.Equal(depois, toma.FamiliaresNotificadosEmUtc);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.False(toma.AtrasoFamiliaresDevido(depois.AddMinutes(1)));
    }

    /// <summary>
    /// RF-110 CA2 e RF-113 CA2: uma toma confirmada, dentro ou fora da tolerância, já não gera avisos.
    /// </summary>
    [Fact]
    public void TomaConfirmadaNaoGeraAvisos()
    {
        var calendario = new CalendarioLisboa();
        var noPrazo = Nova(GrauImportancia.Grave);
        var comAtraso = Nova(GrauImportancia.Grave);

        noPrazo.Confirmar(CuidadorId, Prevista.AddMinutes(20), calendario);
        comAtraso.Confirmar(CuidadorId, Prevista.AddMinutes(45), calendario);

        Assert.False(noPrazo.AtrasoCuidadoresDevido(Prevista.AddMinutes(31)));
        Assert.False(comAtraso.AtrasoFamiliaresDevido(Prevista.AddMinutes(61)));
        comAtraso.MarcarFamiliaresNotificados(Prevista.AddMinutes(61));
        Assert.Null(comAtraso.FamiliaresNotificadosEmUtc);
    }

    /// <summary>
    /// TR-06: uma toma já não confirmada (24 horas) não gera avisos.
    /// </summary>
    [Fact]
    public void TomaNaoConfirmadaNaoGeraAvisos()
    {
        var toma = Nova(GrauImportancia.Grave);
        toma.MarcarNaoConfirmada(Prevista.AddHours(24));

        Assert.False(toma.AtrasoCuidadoresDevido(Prevista.AddHours(25)));
        Assert.False(toma.AtrasoFamiliaresDevido(Prevista.AddHours(25)));
    }

    private static Toma Nova(GrauImportancia grau)
    {
        return new Toma(Guid.NewGuid(), Prevista, "Insulina", "10 UI", grau);
    }
}
