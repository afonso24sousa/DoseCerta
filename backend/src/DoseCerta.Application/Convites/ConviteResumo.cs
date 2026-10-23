// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Convites;

/// <summary>
/// Convite com o nome da ficha e de quem convidou, como é devolvido ao Cuidador que convida e ao convidado (OP-19, OP-20).
/// </summary>
/// <param name="Id">Identificador do convite.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente da ficha.</param>
/// <param name="PapelOferecido">Papel oferecido.</param>
/// <param name="ParaPrincipal">Indica se o convite é para Cuidador principal.</param>
/// <param name="ConvidadoPor">Nome de quem convidou.</param>
/// <param name="Estado">Estado do convite.</param>
/// <param name="CriadoEmUtc">Instante de criação, em UTC.</param>
public sealed record ConviteResumo(
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    Papel PapelOferecido,
    bool ParaPrincipal,
    string ConvidadoPor,
    EstadoConvite Estado,
    DateTime CriadoEmUtc);
