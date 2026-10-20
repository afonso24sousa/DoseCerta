// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Definição do Cuidador principal no pedido de criação de uma ficha de terceiro (OP-09, campo <c>cuidadorPrincipal</c>).
/// </summary>
/// <param name="Eu"><see langword="true"/>: o criador é o Cuidador principal; <see langword="false"/>: é convidada a pessoa do <paramref name="Email"/>.</param>
/// <param name="Email">E-mail da pessoa a convidar para Cuidador principal.</param>
public sealed record PedidoCuidadorPrincipal(
    bool? Eu,
    string? Email);
