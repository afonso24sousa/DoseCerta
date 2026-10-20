// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Pedido de edição de ficha (OP-11). Em <c>multipart/form-data</c> vai na parte <c>dados</c>.
/// </summary>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento, AAAA-MM-DD, não futura.</param>
/// <param name="ContactoEmergencia">Contacto de emergência.</param>
/// <param name="GrauAutonomiaId">Grau de autonomia ou mobilidade, da lista de OP-08.</param>
/// <param name="CondicoesSaudeIds">Condições de saúde, da lista de OP-08.</param>
/// <param name="AlergiasIds">Alergias, da lista de OP-08.</param>
public sealed record PedidoEdicaoFicha(
    string? NomeCompleto,
    DateOnly? DataNascimento,
    string? ContactoEmergencia,
    short? GrauAutonomiaId,
    IReadOnlyList<short>? CondicoesSaudeIds,
    IReadOnlyList<short>? AlergiasIds);
