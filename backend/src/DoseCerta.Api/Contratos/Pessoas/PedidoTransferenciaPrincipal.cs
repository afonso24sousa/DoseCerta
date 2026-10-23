// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Pessoas;

/// <summary>
/// Pedido de transferência do papel de Cuidador principal (OP-26).
/// </summary>
/// <param name="UtilizadorId">Identificador do Cuidador da ficha que passa a Cuidador principal.</param>
public sealed record PedidoTransferenciaPrincipal(Guid? UtilizadorId);
