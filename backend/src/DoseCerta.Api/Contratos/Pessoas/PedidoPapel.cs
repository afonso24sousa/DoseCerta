// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Pessoas;

/// <summary>
/// Pedido de alteração do papel de uma pessoa na ficha (OP-23 promover, OP-24 despromover).
/// </summary>
/// <param name="Papel">Papel pretendido: <c>Cuidador</c> ou <c>Familiar</c>.</param>
public sealed record PedidoPapel(Papel? Papel);
