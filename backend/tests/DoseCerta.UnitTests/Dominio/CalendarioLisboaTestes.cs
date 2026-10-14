// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Tempo;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários de <see cref="CalendarioLisboa"/> (RNF-047, RNF-029).
/// </summary>
public class CalendarioLisboaTestes
{
    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// No horário de verão, as 08:00 de Lisboa correspondem às 07:00 UTC.
    /// </summary>
    [Fact]
    public void ParaUtcNoVeraoSubtraiUmaHora()
    {
        var utc = this.calendario.ParaUtc(new DateOnly(2026, 7, 1), new TimeOnly(8, 0));

        Assert.Equal(new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    /// <summary>
    /// No inverno, a hora de Lisboa coincide com UTC.
    /// </summary>
    [Fact]
    public void ParaUtcNoInvernoMantemAHora()
    {
        var utc = this.calendario.ParaUtc(new DateOnly(2026, 12, 1), new TimeOnly(8, 0));

        Assert.Equal(new DateTime(2026, 12, 1, 8, 0, 0, DateTimeKind.Utc), utc);
    }

    /// <summary>
    /// Na hora repetida da mudança para o inverno usa a primeira ocorrência (ainda em horário de verão).
    /// </summary>
    [Fact]
    public void ParaUtcNaHoraRepetidaUsaAPrimeiraOcorrencia()
    {
        var utc = this.calendario.ParaUtc(new DateOnly(2026, 10, 25), new TimeOnly(1, 30));

        Assert.Equal(new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc), utc);
    }

    /// <summary>
    /// Na hora inexistente da mudança para o verão avança pela duração do salto.
    /// </summary>
    [Fact]
    public void ParaUtcNaHoraInexistenteAvancaOSalto()
    {
        var utc = this.calendario.ParaUtc(new DateOnly(2026, 3, 29), new TimeOnly(1, 30));

        Assert.Equal(new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc), utc);
    }

    /// <summary>
    /// O dia local de um instante perto da meia-noite segue a hora de Lisboa.
    /// </summary>
    [Fact]
    public void DiaLocalSegueAHoraDeLisboa()
    {
        var dia = this.calendario.DiaLocal(new DateTime(2026, 7, 1, 23, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 7, 2), dia);
    }

    /// <summary>
    /// O início do dia seguinte está 24 horas depois do início do dia, fora das mudanças de hora.
    /// </summary>
    [Fact]
    public void InicioDoDiaSeguinteEstaVinteQuatroHorasDepois()
    {
        var dia = new DateOnly(2026, 7, 1);

        var duracao = this.calendario.InicioDoDiaSeguinteUtc(dia) - this.calendario.InicioDoDiaUtc(dia);

        Assert.Equal(TimeSpan.FromHours(24), duracao);
    }

    /// <summary>
    /// A conversão para hora local inclui o desvio de Lisboa.
    /// </summary>
    [Fact]
    public void ParaLocalIncluiODesvio()
    {
        var local = this.calendario.ParaLocal(new DateTime(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TimeSpan.FromHours(1), local.Offset);
        Assert.Equal(8, local.Hour);
    }

    /// <summary>
    /// A formatação usa dd/mm/aaaa e hh:mm em hora de Lisboa.
    /// </summary>
    [Fact]
    public void FormatarDataHoraUsaOFormatoPortugues()
    {
        var texto = this.calendario.FormatarDataHora(new DateTime(2026, 10, 7, 7, 5, 0, DateTimeKind.Utc));

        Assert.Equal("07/10/2026 08:05", texto);
    }
}
