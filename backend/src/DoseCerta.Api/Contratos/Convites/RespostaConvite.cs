// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Convites;

/// <summary>
/// Convite para uma ficha (OP-19 e OP-20).
/// </summary>
/// <param name="Id">Identificador do convite.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente da ficha.</param>
/// <param name="PapelOferecido">Papel oferecido: <c>Cuidador</c> ou <c>Familiar</c>.</param>
/// <param name="ParaPrincipal">Indica se o convite é para Cuidador principal.</param>
/// <param name="ConvidadoPor">Nome de quem convidou.</param>
/// <param name="Estado">Estado do convite.</param>
/// <param name="CriadoEm">Instante de criação, com o desvio de Lisboa.</param>
public sealed record RespostaConvite(
    Guid Id,
    Guid FichaId,
    string NomeFicha,
    Papel PapelOferecido,
    bool ParaPrincipal,
    string ConvidadoPor,
    EstadoConvite Estado,
    DateTimeOffset CriadoEm);
