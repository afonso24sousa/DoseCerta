// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Ficha na lista de fichas do Utilizador (OP-07).
/// </summary>
/// <param name="Id">Identificador da ficha.</param>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="EProprio">Indica se é a ficha própria de um Utilizador.</param>
/// <param name="Estado">Estado da ficha: <c>Ativa</c> ou <c>Arquivada</c>.</param>
/// <param name="MeuPapel">Papel do Utilizador na ficha.</param>
/// <param name="SouPrincipal">Indica se o Utilizador é o Cuidador principal.</param>
public sealed record RespostaResumoFicha(
    Guid Id,
    string NomeCompleto,
    bool EProprio,
    EstadoFicha Estado,
    Papel MeuPapel,
    bool SouPrincipal);
