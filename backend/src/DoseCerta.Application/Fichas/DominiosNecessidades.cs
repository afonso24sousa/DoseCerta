// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Listas fechadas de necessidades específicas (OP-08, RF-021).
/// </summary>
/// <param name="CondicoesSaude">Condições de saúde.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="GrausAutonomia">Graus de autonomia ou mobilidade.</param>
public sealed record DominiosNecessidades(
    IReadOnlyList<CondicaoSaude> CondicoesSaude,
    IReadOnlyList<Alergia> Alergias,
    IReadOnlyList<GrauAutonomia> GrausAutonomia);
