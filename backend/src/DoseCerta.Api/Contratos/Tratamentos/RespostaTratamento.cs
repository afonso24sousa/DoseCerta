// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Tratamentos;

/// <summary>
/// Resposta de OP-27 a OP-31 com os dados de um tratamento.
/// </summary>
/// <param name="Id">Identificador do tratamento.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="Medicamento">Nome do medicamento.</param>
/// <param name="Dose">Dose.</param>
/// <param name="GrauImportancia">Grau de importância.</param>
/// <param name="DataInicio">Data de início.</param>
/// <param name="DataFim">Data de fim, se existir.</param>
/// <param name="Horarios">Horas fixas de toma por dia, no formato <c>HH:mm</c>, por ordem.</param>
/// <param name="Estado">Estado: <c>EmCurso</c> ou <c>Terminado</c>.</param>
/// <param name="ReceitaDocumentoId">Receita mais recente, se existir; obtém-se com OP-17.</param>
public sealed record RespostaTratamento(
    Guid Id,
    Guid FichaId,
    string Medicamento,
    string Dose,
    GrauImportancia GrauImportancia,
    DateOnly DataInicio,
    DateOnly? DataFim,
    IReadOnlyList<string> Horarios,
    EstadoTratamento Estado,
    Guid? ReceitaDocumentoId);
