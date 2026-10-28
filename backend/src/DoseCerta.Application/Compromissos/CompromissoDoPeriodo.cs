// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Compromissos;

/// <summary>
/// Compromisso lido para a lista diária, com o nome do utente da ficha (RF-100, RF-101). Não tem estado de realização (RF-087).
/// </summary>
/// <param name="Id">Identificador do compromisso.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome do utente da ficha.</param>
/// <param name="Motivo">Motivo do compromisso.</param>
/// <param name="DataHoraUtc">Data e hora, em UTC.</param>
public sealed record CompromissoDoPeriodo(
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    string Motivo,
    DateTime DataHoraUtc);
