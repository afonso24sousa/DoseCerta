// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json.Serialization;
using DoseCerta.Application.Agenda;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Agenda;

/// <summary>
/// Evento da resposta de OP-45.
/// </summary>
/// <param name="Id">Identificador da toma ou do compromisso.</param>
/// <param name="Tipo">Tipo: <c>Toma</c> ou <c>Compromisso</c>.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente (RF-089).</param>
/// <param name="Titulo">Medicamento e dose da toma, ou motivo do compromisso.</param>
/// <param name="Inicio">Instante do evento, com o desvio de Lisboa.</param>
/// <param name="Estado">Estado da toma; não existe nos compromissos.</param>
/// <param name="Confirmavel">Se o Utilizador pode confirmar o evento (OP-33); sempre falso nos compromissos (RF-087).</param>
public sealed record RespostaItemAgenda(
    Guid Id,
    TipoItemAgenda Tipo,
    Guid FichaId,
    string NomeFicha,
    string Titulo,
    DateTimeOffset Inicio,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EstadoToma? Estado,
    bool Confirmavel);
