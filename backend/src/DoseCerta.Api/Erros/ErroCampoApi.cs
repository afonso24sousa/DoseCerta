// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Erros;

/// <summary>
/// Campo em erro e regra violada, com o valor ou formato esperado.
/// </summary>
/// <param name="Campo">Nome do campo em camelCase.</param>
/// <param name="Regra">Regra violada.</param>
public sealed record ErroCampoApi(string Campo, string Regra);
