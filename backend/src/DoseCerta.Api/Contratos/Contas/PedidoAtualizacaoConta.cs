// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Corpo do pedido de atualização dos dados da conta (OP-05).
/// </summary>
/// <param name="Nome">Novo nome.</param>
/// <param name="Email">Novo e-mail.</param>
public sealed record PedidoAtualizacaoConta(
    string? Nome,
    string? Email);
