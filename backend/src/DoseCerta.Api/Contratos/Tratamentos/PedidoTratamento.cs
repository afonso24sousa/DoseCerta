// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Tratamentos;

/// <summary>
/// Corpo de OP-27 (registar tratamento) e de OP-30 (atualizar tratamento). Na atualização, um campo em falta mantém o valor.
/// </summary>
/// <param name="Medicamento">Nome do medicamento.</param>
/// <param name="Dose">Dose.</param>
/// <param name="GrauImportancia">Grau de importância: <c>Leve</c>, <c>Moderado</c> ou <c>Grave</c>.</param>
/// <param name="DataInicio">Data de início (AAAA-MM-DD); só no registo.</param>
/// <param name="DataFim">Data de fim (AAAA-MM-DD), opcional.</param>
/// <param name="Horarios">Horas fixas de toma por dia, no formato <c>HH:mm</c>.</param>
public sealed record PedidoTratamento(
    string? Medicamento,
    string? Dose,
    string? GrauImportancia,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    IReadOnlyList<string>? Horarios);
