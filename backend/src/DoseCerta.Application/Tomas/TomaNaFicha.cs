// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Toma com a ficha a que pertence, lida para a agenda (OP-45).
/// </summary>
/// <param name="FichaId">Identificador da ficha do tratamento.</param>
/// <param name="Toma">Toma.</param>
public sealed record TomaNaFicha(Guid FichaId, Toma Toma);
