// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Listas fechadas de necessidades específicas (OP-08).
/// </summary>
/// <param name="CondicoesSaude">Condições de saúde.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="GrausAutonomia">Graus de autonomia ou mobilidade.</param>
public sealed record RespostaDominios(
    IReadOnlyList<RespostaItemDominio> CondicoesSaude,
    IReadOnlyList<RespostaItemDominio> Alergias,
    IReadOnlyList<RespostaItemDominio> GrausAutonomia);
