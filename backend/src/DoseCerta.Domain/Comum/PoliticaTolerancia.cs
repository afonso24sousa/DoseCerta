// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Tolerâncias fixas da confirmação de uma toma e limites de tempo do diagrama de estados da toma (RF-065 a RF-068).
/// </summary>
public static class PoliticaTolerancia
{
    /// <summary>
    /// Tolerância de uma toma de grau Leve.
    /// </summary>
    public static readonly TimeSpan Leve = TimeSpan.FromHours(2);

    /// <summary>
    /// Tolerância de uma toma de grau Moderado.
    /// </summary>
    public static readonly TimeSpan Moderado = TimeSpan.FromHours(1);

    /// <summary>
    /// Tolerância de uma toma de grau Grave.
    /// </summary>
    public static readonly TimeSpan Grave = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Tempo depois da hora prevista em que os Familiares são avisados de uma toma Grave por confirmar.
    /// </summary>
    public static readonly TimeSpan FamiliaresGrave = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Tempo depois da hora prevista em que uma toma pendente passa a não confirmada (RF-068).
    /// </summary>
    public static readonly TimeSpan LimiteNaoConfirmada = TimeSpan.FromHours(24);

    /// <summary>
    /// Obtém a tolerância de um grau de importância.
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    /// <returns>A tolerância: Leve 2 h, Moderado 1 h, Grave 30 min.</returns>
    public static TimeSpan ToleranciaDoGrau(GrauImportancia grau)
    {
        return grau switch
        {
            GrauImportancia.Leve => Leve,
            GrauImportancia.Moderado => Moderado,
            GrauImportancia.Grave => Grave,
            _ => throw new ArgumentOutOfRangeException(nameof(grau), grau, "Grau de importância desconhecido."),
        };
    }

    /// <summary>
    /// Obtém o fim da tolerância de uma toma; uma confirmação até este instante, inclusive, fica <c>Confirmada</c>.
    /// </summary>
    /// <param name="previstaUtc">Instante previsto, em UTC.</param>
    /// <param name="grau">Grau de importância.</param>
    /// <returns>O instante previsto mais a tolerância do grau.</returns>
    public static DateTime FimTolerancia(DateTime previstaUtc, GrauImportancia grau)
    {
        return previstaUtc + ToleranciaDoGrau(grau);
    }

    /// <summary>
    /// Obtém o instante em que os Familiares são avisados de uma toma Grave por confirmar.
    /// </summary>
    /// <param name="previstaUtc">Instante previsto, em UTC.</param>
    /// <returns>O instante previsto mais 60 minutos.</returns>
    public static DateTime InstanteFamiliaresGrave(DateTime previstaUtc)
    {
        return previstaUtc + FamiliaresGrave;
    }

    /// <summary>
    /// Obtém o instante a partir do qual uma toma pendente passa a não confirmada, inclusive (RF-068).
    /// </summary>
    /// <param name="previstaUtc">Instante previsto, em UTC.</param>
    /// <returns>O instante previsto mais 24 horas.</returns>
    public static DateTime InstanteNaoConfirmada(DateTime previstaUtc)
    {
        return previstaUtc + LimiteNaoConfirmada;
    }
}
