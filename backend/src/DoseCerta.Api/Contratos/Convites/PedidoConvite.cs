// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Convites;

/// <summary>
/// Pedido de convite de uma pessoa para a ficha (OP-19).
/// </summary>
/// <param name="Email">E-mail da pessoa convidada.</param>
/// <param name="Papel">Papel oferecido: <c>Cuidador</c> ou <c>Familiar</c>.</param>
/// <param name="ParaPrincipal">Indica se o convite é para Cuidador principal; por omissão <see langword="false"/>.</param>
public sealed record PedidoConvite(
    string? Email,
    Papel? Papel,
    bool? ParaPrincipal);
