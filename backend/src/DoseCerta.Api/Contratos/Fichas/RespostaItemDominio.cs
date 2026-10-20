// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Valor de uma lista fechada de necessidades específicas (OP-08, RF-021).
/// </summary>
/// <param name="Id">Identificador do valor.</param>
/// <param name="Designacao">Designação apresentada.</param>
public sealed record RespostaItemDominio(
    short Id,
    string Designacao);
