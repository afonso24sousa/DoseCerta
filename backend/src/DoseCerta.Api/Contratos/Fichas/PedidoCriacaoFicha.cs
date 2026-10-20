// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Pedido de criação de ficha de terceiro ou própria (OP-09). Em <c>multipart/form-data</c> vai na parte <c>dados</c>.
/// </summary>
/// <param name="Propria"><see langword="true"/> para a ficha própria do Utilizador (RF-030); por omissão, ficha de terceiro.</param>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento, AAAA-MM-DD, não futura.</param>
/// <param name="ContactoEmergencia">Contacto de emergência.</param>
/// <param name="GrauAutonomiaId">Grau de autonomia ou mobilidade, da lista de OP-08.</param>
/// <param name="CondicoesSaudeIds">Condições de saúde, da lista de OP-08.</param>
/// <param name="AlergiasIds">Alergias, da lista de OP-08.</param>
/// <param name="CuidadorPrincipal">Cuidador principal, obrigatório na ficha de terceiro (RF-024, RF-025).</param>
public sealed record PedidoCriacaoFicha(
    bool? Propria,
    string? NomeCompleto,
    DateOnly? DataNascimento,
    string? ContactoEmergencia,
    short? GrauAutonomiaId,
    IReadOnlyList<short>? CondicoesSaudeIds,
    IReadOnlyList<short>? AlergiasIds,
    PedidoCuidadorPrincipal? CuidadorPrincipal);
