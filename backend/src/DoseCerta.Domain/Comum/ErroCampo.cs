// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Indica o campo em erro e a regra violada, com o valor ou formato esperado (RNF-028).
/// </summary>
/// <param name="Campo">Nome do campo no corpo do pedido, em camelCase.</param>
/// <param name="Regra">Regra violada, com o valor ou o formato esperado.</param>
public sealed record ErroCampo(string Campo, string Regra);
