// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Tomas;

/// <summary>
/// Resposta de OP-34 com uma toma pendente do calendário a sincronizar no telemóvel.
/// </summary>
/// <param name="TomaId">Identificador da toma.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente.</param>
/// <param name="Medicamento">Medicamento.</param>
/// <param name="Dose">Dose.</param>
/// <param name="PrevistaEm">Instante previsto, com o desvio de Lisboa.</param>
public sealed record RespostaTomaSincronizada(
    Guid TomaId,
    Guid FichaId,
    string NomeFicha,
    string Medicamento,
    string Dose,
    DateTimeOffset PrevistaEm);
