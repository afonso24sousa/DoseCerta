// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Registos;

/// <summary>
/// Corpo de OP-47 (registar nota partilhada).
/// </summary>
/// <param name="Descricao">Texto da nota.</param>
public sealed record PedidoNota(string? Descricao);
