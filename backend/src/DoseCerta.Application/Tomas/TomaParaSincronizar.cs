// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Toma pendente do calendário a sincronizar no telemóvel, com o nome da ficha (OP-34, RF-061, RF-062).
/// </summary>
/// <param name="TomaId">Identificador da toma.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente.</param>
/// <param name="Medicamento">Medicamento.</param>
/// <param name="Dose">Dose.</param>
/// <param name="PrevistaEmUtc">Instante previsto, em UTC.</param>
public sealed record TomaParaSincronizar(
    Guid TomaId,
    Guid FichaId,
    string NomeFicha,
    string Medicamento,
    string Dose,
    DateTime PrevistaEmUtc);
