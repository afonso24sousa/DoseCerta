// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários do comportamento de <see cref="Toma"/>, segundo o diagrama de estados da toma (TR-03, TR-04, TR-06, TR-13 a TR-15).
/// Usa uma toma em hora de verão (7 de outubro, 08:00 de Lisboa = 07:00 UTC) e outra em hora de inverno (10 de novembro, 08:00 = 08:00 UTC).
/// </summary>
public class TomaTestes
{
    private static readonly DateTime PrevistaVerao = new(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PrevistaInverno = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly CalendarioLisboa calendario = new();
    private readonly Guid cuidador = Guid.NewGuid();

    /// <summary>
    /// TR-03 e RF-065: dentro da tolerância, incluindo o instante exato do fim, fica <c>Confirmada</c> com o autor e o instante.
    /// </summary>
    /// <param name="minutosDepois">Minutos depois da hora prevista.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(60)]
    public void ConfirmarDentroDaToleranciaFicaConfirmada(int minutosDepois)
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Moderado);
        var agora = PrevistaVerao.AddMinutes(minutosDepois);

        toma.Confirmar(this.cuidador, agora, this.calendario);

        Assert.Equal(EstadoToma.Confirmada, toma.Estado);
        Assert.Equal(this.cuidador, toma.ConfirmadaPorId);
        Assert.Equal(agora, toma.ConfirmadaEmUtc);
        Assert.True(toma.EstaConfirmada());
    }

    /// <summary>
    /// TR-03: a janela abre às 00:00 de Lisboa do dia da toma, que na hora de verão é 23:00 UTC do dia anterior.
    /// </summary>
    [Fact]
    public void ConfirmarAntesDaHoraNoMesmoDiaFicaConfirmada()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Moderado);
        var meiaNoiteLisboa = new DateTime(2026, 10, 6, 23, 0, 0, DateTimeKind.Utc);

        Assert.True(toma.JanelaDeConfirmacaoAberta(meiaNoiteLisboa, this.calendario));
        toma.Confirmar(this.cuidador, meiaNoiteLisboa, this.calendario);

        Assert.Equal(EstadoToma.Confirmada, toma.Estado);
    }

    /// <summary>
    /// TR-04 e RF-066: um segundo depois do fim da tolerância fica <c>ConfirmadaComAtraso</c>, em cada grau.
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    /// <param name="minutos">Tolerância do grau, em minutos.</param>
    [Theory]
    [InlineData(GrauImportancia.Leve, 120)]
    [InlineData(GrauImportancia.Moderado, 60)]
    [InlineData(GrauImportancia.Grave, 30)]
    public void ConfirmarDepoisDaToleranciaFicaComAtraso(GrauImportancia grau, int minutos)
    {
        var noLimite = Nova(PrevistaInverno, grau);
        var depois = Nova(PrevistaInverno, grau);

        noLimite.Confirmar(this.cuidador, PrevistaInverno.AddMinutes(minutos), this.calendario);
        depois.Confirmar(this.cuidador, PrevistaInverno.AddMinutes(minutos).AddSeconds(1), this.calendario);

        Assert.Equal(EstadoToma.Confirmada, noLimite.Estado);
        Assert.Equal(EstadoToma.ConfirmadaComAtraso, depois.Estado);
        Assert.Equal(PrevistaInverno.AddMinutes(minutos), depois.FimTolerancia());
    }

    /// <summary>
    /// TR-04: pouco antes das 24 horas ainda se confirma, com atraso.
    /// </summary>
    [Fact]
    public void ConfirmarAntesDasVinteEQuatroHorasFicaComAtraso()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Grave);

        toma.Confirmar(this.cuidador, PrevistaVerao.AddHours(24).AddSeconds(-1), this.calendario);

        Assert.Equal(EstadoToma.ConfirmadaComAtraso, toma.Estado);
    }

    /// <summary>
    /// TR-13: às 23:59 do dia anterior (Lisboa) recusa com <c>TOMA_FORA_DA_JANELA</c> e a toma fica pendente, no verão e no inverno.
    /// </summary>
    [Fact]
    public void ConfirmarNoDiaAnteriorFicaForaDaJanela()
    {
        var verao = Nova(PrevistaVerao, GrauImportancia.Moderado);
        var inverno = Nova(PrevistaInverno, GrauImportancia.Moderado);

        var erroVerao = Assert.Throws<DominioException>(() =>
            verao.Confirmar(this.cuidador, new DateTime(2026, 10, 6, 22, 59, 0, DateTimeKind.Utc), this.calendario));
        var erroInverno = Assert.Throws<DominioException>(() =>
            inverno.Confirmar(this.cuidador, new DateTime(2026, 11, 9, 23, 59, 59, DateTimeKind.Utc), this.calendario));

        Assert.Equal(CodigosErro.TomaForaDaJanela, erroVerao.Codigo);
        Assert.Equal(CodigosErro.TomaForaDaJanela, erroInverno.Codigo);
        Assert.Equal(EstadoToma.Pendente, verao.Estado);
        Assert.Null(verao.ConfirmadaPorId);
        Assert.True(inverno.JanelaDeConfirmacaoAberta(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), this.calendario));
    }

    /// <summary>
    /// TR-06: <see cref="Toma.MarcarNaoConfirmada"/> só muda o estado a partir das 24 horas, inclusive.
    /// </summary>
    [Fact]
    public void MarcarNaoConfirmadaSoAPartirDasVinteEQuatroHoras()
    {
        var antes = Nova(PrevistaVerao, GrauImportancia.Leve);
        var noLimite = Nova(PrevistaVerao, GrauImportancia.Leve);
        var limite = PrevistaVerao.AddHours(24);

        antes.MarcarNaoConfirmada(limite.AddSeconds(-1));
        noLimite.MarcarNaoConfirmada(limite);

        Assert.Equal(EstadoToma.Pendente, antes.Estado);
        Assert.Null(antes.NaoConfirmadaDetetadaEmUtc);
        Assert.Equal(EstadoToma.NaoConfirmada, noLimite.Estado);
        Assert.Equal(limite, noLimite.NaoConfirmadaDetetadaEmUtc);
        Assert.Equal(limite, noLimite.InstanteNaoConfirmada());
    }

    /// <summary>
    /// TR-06: uma toma já confirmada não passa a não confirmada.
    /// </summary>
    [Fact]
    public void MarcarNaoConfirmadaNaoAlteraUmaTomaConfirmada()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Leve);
        toma.Confirmar(this.cuidador, PrevistaVerao, this.calendario);

        toma.MarcarNaoConfirmada(PrevistaVerao.AddDays(2));

        Assert.Equal(EstadoToma.Confirmada, toma.Estado);
        Assert.Null(toma.NaoConfirmadaDetetadaEmUtc);
    }

    /// <summary>
    /// TR-14: uma toma já <c>NaoConfirmada</c> não pode ser confirmada.
    /// </summary>
    [Fact]
    public void ConfirmarUmaTomaNaoConfirmadaERecusado()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Moderado);
        toma.MarcarNaoConfirmada(PrevistaVerao.AddHours(25));

        var erro = Assert.Throws<DominioException>(() => toma.Confirmar(this.cuidador, PrevistaVerao.AddHours(26), this.calendario));

        Assert.Equal(CodigosErro.TomaNaoConfirmavel, erro.Codigo);
        Assert.Equal(PrevistaVerao.AddHours(25), toma.NaoConfirmadaDetetadaEmUtc);
    }

    /// <summary>
    /// TR-06 e TR-14: uma toma ainda pendente com 24 horas passa a <c>NaoConfirmada</c> e a confirmação é recusada.
    /// </summary>
    [Fact]
    public void ConfirmarUmaTomaPendenteComVinteEQuatroHorasERecusado()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Moderado);
        var agora = PrevistaVerao.AddHours(24);

        var erro = Assert.Throws<DominioException>(() => toma.Confirmar(this.cuidador, agora, this.calendario));

        Assert.Equal(CodigosErro.TomaNaoConfirmavel, erro.Codigo);
        Assert.Equal(EstadoToma.NaoConfirmada, toma.Estado);
        Assert.Null(toma.ConfirmadaPorId);
    }

    /// <summary>
    /// TR-15: uma segunda confirmação é recusada e mantém a primeira.
    /// </summary>
    [Fact]
    public void SegundaConfirmacaoMantemAPrimeira()
    {
        var toma = Nova(PrevistaVerao, GrauImportancia.Moderado);
        toma.Confirmar(this.cuidador, PrevistaVerao, this.calendario);

        var erro = Assert.Throws<DominioException>(() => toma.Confirmar(Guid.NewGuid(), PrevistaVerao.AddMinutes(10), this.calendario));

        Assert.Equal(CodigosErro.TomaJaConfirmada, erro.Codigo);
        Assert.Equal(this.cuidador, toma.ConfirmadaPorId);
        Assert.Equal(PrevistaVerao, toma.ConfirmadaEmUtc);
    }

    private static Toma Nova(DateTime previstaUtc, GrauImportancia grau)
    {
        return new Toma(Guid.NewGuid(), previstaUtc, "Metformina", "500 mg", grau);
    }
}
