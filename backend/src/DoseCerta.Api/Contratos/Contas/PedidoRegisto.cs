// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Corpo do pedido de criação de conta (OP-01).
/// </summary>
/// <param name="Nome">Nome da pessoa.</param>
/// <param name="Email">E-mail da conta.</param>
/// <param name="PalavraPasse">Palavra-passe (RNF-003).</param>
public sealed record PedidoRegisto(
    string? Nome,
    string? Email,
    string? PalavraPasse);
