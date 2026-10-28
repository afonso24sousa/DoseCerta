// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Registos;

/// <summary>
/// Corpo de OP-46 (registar ocorrência).
/// </summary>
/// <param name="Descricao">Descrição da ocorrência.</param>
/// <param name="OcorridaEm">Data e hora em que a ocorrência aconteceu, com o desvio de Lisboa.</param>
public sealed record PedidoOcorrencia(string? Descricao, DateTimeOffset? OcorridaEm);
