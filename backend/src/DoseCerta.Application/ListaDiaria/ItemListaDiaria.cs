// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.ListaDiaria;

/// <summary>
/// Item da lista diária consolidada: uma toma ou um compromisso, com o nome do utente da ficha (RF-100, RF-101, RF-104).
/// </summary>
/// <param name="Tipo">Tipo do item.</param>
/// <param name="Id">Identificador da toma ou do compromisso.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome do utente da ficha.</param>
/// <param name="Titulo">Medicamento e dose da toma, ou motivo do compromisso.</param>
/// <param name="PrevistaEmUtc">Instante previsto, em UTC.</param>
/// <param name="Estado">Estado da toma; <see langword="null"/> num compromisso.</param>
/// <param name="ConfirmadaPor">Nome de quem confirmou a toma, ou <see langword="null"/>.</param>
/// <param name="ConfirmadaEmUtc">Instante da confirmação, em UTC, ou <see langword="null"/>.</param>
/// <param name="Confirmavel">Indica se o item pode ser confirmado a partir da lista (RF-087, RF-102).</param>
public sealed record ItemListaDiaria(
    TipoItemListaDiaria Tipo,
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    string Titulo,
    DateTime PrevistaEmUtc,
    EstadoToma? Estado,
    string? ConfirmadaPor,
    DateTime? ConfirmadaEmUtc,
    bool Confirmavel);
