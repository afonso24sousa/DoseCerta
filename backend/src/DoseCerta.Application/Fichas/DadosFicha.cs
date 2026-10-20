// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Dados de identificação e necessidades específicas submetidos na criação (OP-09) e na edição (OP-11) de uma ficha.
/// Os valores chegam como foram enviados e são validados pelo <see cref="FichaService"/>.
/// </summary>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="ContactoEmergencia">Contacto de emergência.</param>
/// <param name="GrauAutonomiaId">Grau de autonomia ou mobilidade, opcional.</param>
/// <param name="CondicoesSaudeIds">Condições de saúde, da lista fechada.</param>
/// <param name="AlergiasIds">Alergias, da lista fechada.</param>
public sealed record DadosFicha(
    string? NomeCompleto,
    DateOnly? DataNascimento,
    string? ContactoEmergencia,
    short? GrauAutonomiaId,
    IReadOnlyCollection<short>? CondicoesSaudeIds,
    IReadOnlyCollection<short>? AlergiasIds);
