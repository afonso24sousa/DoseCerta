// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json.Serialization;
using DoseCerta.Application.ListaDiaria;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.ListaDiaria;

/// <summary>
/// Item da resposta de OP-50. Os campos <c>estado</c>, <c>confirmadaPor</c> e <c>confirmadaEm</c> só aparecem quando têm valor,
/// como no exemplo da Documentação da API.
/// </summary>
/// <param name="Tipo">Tipo: <c>Toma</c> ou <c>Compromisso</c>.</param>
/// <param name="Id">Identificador da toma ou do compromisso.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome do utente da ficha (RF-101).</param>
/// <param name="Titulo">Medicamento e dose da toma, ou motivo do compromisso.</param>
/// <param name="PrevistaEm">Instante previsto, com o desvio de Lisboa.</param>
/// <param name="Estado">Estado da toma; não existe nos compromissos.</param>
/// <param name="ConfirmadaPor">Nome de quem confirmou a toma (RF-104).</param>
/// <param name="ConfirmadaEm">Instante da confirmação, com o desvio de Lisboa (RF-104).</param>
/// <param name="Confirmavel">Indica se o item pode ser confirmado com OP-33; é sempre falso nos compromissos (RF-087).</param>
public sealed record RespostaItemListaDiaria(
    TipoItemListaDiaria Tipo,
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    string Titulo,
    DateTimeOffset PrevistaEm,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EstadoToma? Estado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ConfirmadaPor,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ConfirmadaEm,
    bool Confirmavel);
