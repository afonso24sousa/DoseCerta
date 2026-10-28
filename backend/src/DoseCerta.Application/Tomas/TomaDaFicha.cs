// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Toma de uma ficha, com o nome de quem a confirmou (OP-32, OP-33).
/// </summary>
/// <param name="Toma">Toma.</param>
/// <param name="ConfirmadaPorNome">Nome do Cuidador que confirmou, ou <see langword="null"/> se não estiver confirmada.</param>
public sealed record TomaDaFicha(Toma Toma, string? ConfirmadaPorNome);
