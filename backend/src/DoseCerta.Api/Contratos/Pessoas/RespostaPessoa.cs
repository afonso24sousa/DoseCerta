// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Pessoas;

/// <summary>
/// Pessoa associada à ficha (OP-18; também a resposta de OP-21, OP-23, OP-24 e OP-26).
/// </summary>
/// <param name="UtilizadorId">Identificador do Utilizador.</param>
/// <param name="Nome">Nome da conta.</param>
/// <param name="Email">E-mail da conta.</param>
/// <param name="Papel">Papel na ficha: <c>Cuidador</c> ou <c>Familiar</c>.</param>
/// <param name="Principal">Indica se é o Cuidador principal.</param>
/// <param name="Provisorio">Indica se é Cuidador principal provisório, até à resposta ao convite para Cuidador principal (RF-025).</param>
public sealed record RespostaPessoa(
    Guid UtilizadorId,
    string Nome,
    string Email,
    Papel Papel,
    bool Principal,
    bool Provisorio);
