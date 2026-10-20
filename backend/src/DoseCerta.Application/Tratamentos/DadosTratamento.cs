// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Tratamentos;

/// <summary>
/// Dados de um tratamento submetidos ao registar (OP-27) ou ao atualizar (OP-30). Na atualização, um campo em falta
/// mantém o valor atual.
/// </summary>
/// <param name="Medicamento">Nome do medicamento.</param>
/// <param name="Dose">Dose.</param>
/// <param name="GrauImportancia">Grau de importância: <c>Leve</c>, <c>Moderado</c> ou <c>Grave</c>.</param>
/// <param name="DataInicio">Data de início; só no registo.</param>
/// <param name="DataFim">Data de fim, opcional.</param>
/// <param name="Horarios">Horas fixas de toma por dia, no formato <c>HH:mm</c>.</param>
public sealed record DadosTratamento(
    string? Medicamento,
    string? Dose,
    string? GrauImportancia,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    IReadOnlyList<string>? Horarios);
