// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;

namespace DoseCerta.Domain.Tempo;

/// <summary>
/// Cálculos de tempo no fuso Europe/Lisbon (RNF-047, AD-09). Os instantes são guardados em UTC
/// e apresentados com o desvio de Lisboa; as horas fixas e os limites do dia seguem a hora de Lisboa.
/// </summary>
public sealed class CalendarioLisboa
{
    /// <summary>
    /// Identificador IANA do fuso de referência.
    /// </summary>
    public const string IdentificadorFuso = "Europe/Lisbon";

    private static readonly CultureInfo CulturaPortuguesa = CultureInfo.GetCultureInfo("pt-PT");

    private readonly TimeZoneInfo fuso;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarioLisboa"/> class.
    /// </summary>
    public CalendarioLisboa()
    {
        this.fuso = TimeZoneInfo.FindSystemTimeZoneById(IdentificadorFuso);
    }

    /// <summary>
    /// Converte uma hora fixa de um dia, em hora de Lisboa, para o instante UTC correspondente.
    /// Numa hora repetida usa a primeira ocorrência; numa hora inexistente avança pela duração do salto.
    /// </summary>
    /// <param name="dia">Dia em hora de Lisboa.</param>
    /// <param name="horaLocal">Hora fixa em hora de Lisboa.</param>
    /// <returns>O instante em UTC.</returns>
    public DateTime ParaUtc(DateOnly dia, TimeOnly horaLocal)
    {
        var local = DateTime.SpecifyKind(dia.ToDateTime(horaLocal), DateTimeKind.Unspecified);

        if (this.fuso.IsInvalidTime(local))
        {
            var regra = this.fuso.GetAdjustmentRules()
                .FirstOrDefault(r => r.DateStart <= local && r.DateEnd >= local);
            var salto = regra?.DaylightDelta ?? TimeSpan.FromHours(1);
            local = local.Add(salto);
        }

        if (this.fuso.IsAmbiguousTime(local))
        {
            var desvios = this.fuso.GetAmbiguousTimeOffsets(local);
            var maiorDesvio = desvios.Max();
            return DateTime.SpecifyKind(local - maiorDesvio, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, this.fuso);
    }

    /// <summary>
    /// Obtém o dia, em hora de Lisboa, de um instante UTC.
    /// </summary>
    /// <param name="instanteUtc">Instante em UTC.</param>
    /// <returns>O dia em hora de Lisboa.</returns>
    public DateOnly DiaLocal(DateTime instanteUtc)
    {
        return DateOnly.FromDateTime(this.ParaLocal(instanteUtc).DateTime);
    }

    /// <summary>
    /// Obtém o instante UTC correspondente às 00:00 de Lisboa de um dia.
    /// </summary>
    /// <param name="dia">Dia em hora de Lisboa.</param>
    /// <returns>O instante em UTC.</returns>
    public DateTime InicioDoDiaUtc(DateOnly dia)
    {
        return this.ParaUtc(dia, TimeOnly.MinValue);
    }

    /// <summary>
    /// Obtém o instante UTC correspondente às 00:00 de Lisboa do dia seguinte.
    /// </summary>
    /// <param name="dia">Dia em hora de Lisboa.</param>
    /// <returns>O instante em UTC.</returns>
    public DateTime InicioDoDiaSeguinteUtc(DateOnly dia)
    {
        return this.InicioDoDiaUtc(dia.AddDays(1));
    }

    /// <summary>
    /// Converte um instante UTC para a hora de Lisboa, com o respetivo desvio.
    /// </summary>
    /// <param name="instanteUtc">Instante em UTC.</param>
    /// <returns>O instante com o desvio de Lisboa, por exemplo <c>2026-10-07T08:00:00+01:00</c>.</returns>
    public DateTimeOffset ParaLocal(DateTime instanteUtc)
    {
        var utc = DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc);
        var desvio = this.fuso.GetUtcOffset(utc);
        return new DateTimeOffset(utc).ToOffset(desvio);
    }

    /// <summary>
    /// Formata um instante UTC em hora de Lisboa no formato dd/mm/aaaa hh:mm (RNF-029).
    /// </summary>
    /// <param name="instanteUtc">Instante em UTC.</param>
    /// <returns>O texto formatado.</returns>
    public string FormatarDataHora(DateTime instanteUtc)
    {
        return this.ParaLocal(instanteUtc).ToString("dd/MM/yyyy HH:mm", CulturaPortuguesa);
    }
}
