// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Definição do Cuidador principal de uma ficha de terceiro (OP-09, campo <c>cuidadorPrincipal</c>).
/// </summary>
/// <param name="Eu"><see langword="true"/>: o criador é o Cuidador principal (RF-024); <see langword="false"/>: é convidada outra pessoa (RF-025).</param>
/// <param name="Email">E-mail da pessoa a convidar, quando <paramref name="Eu"/> é <see langword="false"/>.</param>
public sealed record DefinicaoPrincipal(bool? Eu, string? Email);
