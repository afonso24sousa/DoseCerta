// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Tomas;

namespace DoseCerta.Api.Erros;

/// <summary>
/// Confirmação já gravada, no corpo do erro <c>TOMA_JA_CONFIRMADA</c> (OP-33, RF-105).
/// </summary>
/// <param name="ConfirmadaPor">Cuidador que confirmou.</param>
/// <param name="ConfirmadaEm">Instante da confirmação, com o desvio de Lisboa.</param>
public sealed record ConfirmacaoApi(PessoaConfirmou ConfirmadaPor, DateTimeOffset ConfirmadaEm);
