// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Agenda;

/// <summary>
/// Evento da agenda: uma toma prevista ou um compromisso de uma ficha do Utilizador (OP-45, RF-083, RF-089).
/// </summary>
/// <param name="Id">Identificador da toma ou do compromisso.</param>
/// <param name="Tipo">Tipo do evento.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente.</param>
/// <param name="Titulo">Medicamento e dose da toma, ou motivo do compromisso.</param>
/// <param name="InicioUtc">Instante do evento, em UTC.</param>
/// <param name="Estado">Estado da toma; <see langword="null"/> nos compromissos.</param>
/// <param name="Confirmavel">Se o Utilizador pode confirmar o evento: só tomas pendentes de fichas em que é Cuidador.</param>
public sealed record ItemAgenda(
    Guid Id,
    TipoItemAgenda Tipo,
    Guid FichaId,
    string NomeFicha,
    string Titulo,
    DateTime InicioUtc,
    EstadoToma? Estado,
    bool Confirmavel);
