// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Confirmação já gravada de uma toma, devolvida na recusa de uma segunda confirmação (TOMA_JA_CONFIRMADA, RF-105).
/// </summary>
/// <param name="ConfirmadaPorId">Identificador do Cuidador que confirmou.</param>
/// <param name="ConfirmadaPorNome">Nome do Cuidador que confirmou.</param>
/// <param name="ConfirmadaEmUtc">Instante da confirmação, em UTC.</param>
public sealed record ConfirmacaoRegistada(Guid ConfirmadaPorId, string ConfirmadaPorNome, DateTime ConfirmadaEmUtc);
