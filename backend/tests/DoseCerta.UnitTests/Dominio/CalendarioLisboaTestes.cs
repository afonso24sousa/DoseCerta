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

    /// <summary>
    /// RNF-047: nas mudanças de hora, o dia de Lisboa tem 23 horas (último domingo de março) ou 25 horas (último domingo de outubro).
    /// </summary>
    [Fact]
    public void DiasDaMudancaDeHoraTem23E25Horas()
    {
        var marco = new DateOnly(2026, 3, 29);
        var outubro = new DateOnly(2026, 10, 25);

        Assert.Equal(new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc), this.calendario.InicioDoDiaUtc(marco));
        Assert.Equal(TimeSpan.FromHours(23), this.calendario.InicioDoDiaSeguinteUtc(marco) - this.calendario.InicioDoDiaUtc(marco));
        Assert.Equal(new DateTime(2026, 10, 24, 23, 0, 0, DateTimeKind.Utc), this.calendario.InicioDoDiaUtc(outubro));
        Assert.Equal(TimeSpan.FromHours(25), this.calendario.InicioDoDiaSeguinteUtc(outubro) - this.calendario.InicioDoDiaUtc(outubro));
    }

    /// <summary>
    /// RF-085: a semana vai de segunda-feira a domingo; a semana do último domingo de outubro começa ainda em hora de verão.
    /// </summary>
    /// <param name="dia">Dia do mês de outubro de 2026 dentro da semana.</param>
    [Theory]
    [InlineData(19)]
    [InlineData(22)]
    [InlineData(25)]
    public void SemanaComecaNaSegundaFeira(int dia)
    {
        var inicio = this.calendario.InicioDaSemanaUtc(new DateOnly(2026, 10, dia));

        Assert.Equal(new DateTime(2026, 10, 18, 23, 0, 0, DateTimeKind.Utc), inicio);
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), this.calendario.InicioDaSemanaUtc(new DateOnly(2026, 10, 26)));
    }

    /// <summary>
    /// RF-085: a semana que atravessa o mês começa na segunda-feira do mês anterior.
    /// </summary>
    [Fact]
    public void SemanaQueAtravessaOMes()
    {
        var inicio = this.calendario.InicioDaSemanaUtc(new DateOnly(2026, 10, 1));

        Assert.Equal(new DateOnly(2026, 9, 28), this.calendario.DiaLocal(inicio));
        Assert.Equal(new DateTime(2026, 9, 27, 23, 0, 0, DateTimeKind.Utc), inicio);
    }

    /// <summary>
    /// RF-086: o mês vai das 00:00 de Lisboa do dia 1 às 00:00 do dia 1 do mês seguinte, com a mudança de hora pelo meio.
    /// </summary>
    [Fact]
    public void MesEmHoraDeLisboa()
    {
        var dia = new DateOnly(2026, 3, 15);

        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), this.calendario.InicioDoMesUtc(dia));
        Assert.Equal(new DateTime(2026, 3, 31, 23, 0, 0, DateTimeKind.Utc), this.calendario.InicioDoMesSeguinteUtc(dia));
        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), this.calendario.InicioDoMesSeguinteUtc(new DateOnly(2026, 12, 31)));
    }
}
