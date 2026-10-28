// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Toma lida para a lista diária, com o nome do utente da ficha e o nome de quem a confirmou (RF-100, RF-101, RF-104).
/// </summary>
/// <param name="Id">Identificador da toma.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome do utente da ficha.</param>
/// <param name="Medicamento">Medicamento copiado na geração da toma.</param>
/// <param name="Dose">Dose copiada na geração da toma.</param>
/// <param name="PrevistaEmUtc">Instante previsto, em UTC.</param>
/// <param name="Estado">Estado da toma.</param>
/// <param name="ConfirmadaPor">Nome do Cuidador que confirmou, ou <see langword="null"/> se não foi confirmada.</param>
/// <param name="ConfirmadaEmUtc">Instante da confirmação, em UTC, ou <see langword="null"/> se não foi confirmada.</param>
public sealed record TomaDoDia(
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    string Medicamento,
    string Dose,
    DateTime PrevistaEmUtc,
    EstadoToma Estado,
    string? ConfirmadaPor,
    DateTime? ConfirmadaEmUtc);
