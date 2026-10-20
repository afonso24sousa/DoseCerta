// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Corpo do pedido de início de sessão (OP-02).
/// </summary>
/// <param name="Email">E-mail da conta.</param>
/// <param name="PalavraPasse">Palavra-passe.</param>
public sealed record PedidoLogin(
    string? Email,
    string? PalavraPasse);
