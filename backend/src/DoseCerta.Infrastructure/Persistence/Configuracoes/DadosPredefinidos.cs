// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Infrastructure.Persistence.Configuracoes;

/// <summary>
/// Valores iniciais das três listas fechadas de necessidades específicas (modelo de dados, secção 8; RF-021).
/// São criados pela migração inicial.
/// </summary>
internal static class DadosPredefinidos
{
    /// <summary>
    /// Obtém a lista inicial de condições de saúde.
    /// </summary>
    /// <returns>As condições de saúde.</returns>
    public static CondicaoSaude[] CondicoesSaude()
    {
        return new[]
        {
            new CondicaoSaude(1, "Diabetes"),
            new CondicaoSaude(2, "Hipertensão arterial"),
            new CondicaoSaude(3, "Doença cardíaca"),
            new CondicaoSaude(4, "Doença respiratória crónica"),
            new CondicaoSaude(5, "Demência ou doença de Alzheimer"),
            new CondicaoSaude(6, "Doença de Parkinson"),
            new CondicaoSaude(7, "Epilepsia"),
            new CondicaoSaude(8, "Insuficiência renal"),
            new CondicaoSaude(9, "Outra condição relevante"),
        };
    }

    /// <summary>
    /// Obtém a lista inicial de alergias.
    /// </summary>
    /// <returns>As alergias.</returns>
    public static Alergia[] Alergias()
    {
        return new[]
        {
            new Alergia(1, "Medicamentos"),
            new Alergia(2, "Alimentos"),
            new Alergia(3, "Látex"),
            new Alergia(4, "Picadas de insetos"),
            new Alergia(5, "Pólen ou ácaros"),
            new Alergia(6, "Outra alergia"),
        };
    }

    /// <summary>
    /// Obtém a lista inicial de graus de autonomia ou mobilidade.
    /// </summary>
    /// <returns>Os graus de autonomia.</returns>
    public static GrauAutonomia[] GrausAutonomia()
    {
        return new[]
        {
            new GrauAutonomia(1, "Autónomo", 1),
            new GrauAutonomia(2, "Autonomia parcial", 2),
            new GrauAutonomia(3, "Dependente", 3),
            new GrauAutonomia(4, "Acamado", 4),
        };
    }
}
