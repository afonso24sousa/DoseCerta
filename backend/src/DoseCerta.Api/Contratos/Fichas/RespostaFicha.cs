// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Dados de identificação e necessidades de uma ficha, com o papel do Utilizador (OP-09, OP-10 e OP-11).
/// </summary>
/// <param name="Id">Identificador da ficha.</param>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="ContactoEmergencia">Contacto de emergência.</param>
/// <param name="EProprio">Indica se é a ficha própria de um Utilizador.</param>
/// <param name="Estado">Estado da ficha: <c>Ativa</c> ou <c>Arquivada</c>.</param>
/// <param name="GrauAutonomia">Grau de autonomia ou mobilidade, ou <see langword="null"/>.</param>
/// <param name="CondicoesSaude">Condições de saúde.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="MeuPapel">Papel do Utilizador na ficha: <c>Cuidador</c> ou <c>Familiar</c>.</param>
/// <param name="SouPrincipal">Indica se o Utilizador é o Cuidador principal (incluindo o provisório).</param>
public sealed record RespostaFicha(
    Guid Id,
    string NomeCompleto,
    DateOnly DataNascimento,
    string ContactoEmergencia,
    bool EProprio,
    EstadoFicha Estado,
    RespostaItemDominio? GrauAutonomia,
    IReadOnlyList<RespostaItemDominio> CondicoesSaude,
    IReadOnlyList<RespostaItemDominio> Alergias,
    Papel MeuPapel,
    bool SouPrincipal);
