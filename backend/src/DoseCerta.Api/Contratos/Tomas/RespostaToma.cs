// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Tomas;

/// <summary>
/// Resposta de OP-32 e OP-33 com os dados de uma toma.
/// </summary>
/// <param name="Id">Identificador da toma.</param>
/// <param name="TratamentoId">Identificador do tratamento.</param>
/// <param name="Medicamento">Medicamento copiado do tratamento.</param>
/// <param name="Dose">Dose copiada do tratamento.</param>
/// <param name="GrauImportancia">Grau de importância copiado do tratamento.</param>
/// <param name="PrevistaEm">Instante previsto, com o desvio de Lisboa.</param>
/// <param name="Estado">Estado da toma.</param>
/// <param name="ConfirmadaPor">Cuidador que confirmou; <see langword="null"/> se não estiver confirmada.</param>
/// <param name="ConfirmadaEm">Instante da confirmação, com o desvio de Lisboa; <see langword="null"/> se não estiver confirmada.</param>
public sealed record RespostaToma(
    Guid Id,
    Guid TratamentoId,
    string Medicamento,
    string Dose,
    GrauImportancia GrauImportancia,
    DateTimeOffset PrevistaEm,
    EstadoToma Estado,
    PessoaConfirmou? ConfirmadaPor,
    DateTimeOffset? ConfirmadaEm);
