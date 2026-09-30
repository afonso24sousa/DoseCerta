// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Categorias fechadas usadas no preenchimento de uma ficha.
/// </summary>
/// <param name="CondicoesSaude">Condições de saúde relevantes.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="GrausAutonomia">Graus de autonomia e mobilidade.</param>
public record RespostaCatalogos(
    IReadOnlyList<RespostaCategoria> CondicoesSaude,
    IReadOnlyList<RespostaCategoria> Alergias,
    IReadOnlyList<RespostaCategoria> GrausAutonomia);
