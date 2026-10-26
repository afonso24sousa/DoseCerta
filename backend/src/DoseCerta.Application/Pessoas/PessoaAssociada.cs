// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Pessoas;

/// <summary>
/// Pessoa associada a uma ficha, com os dados da conta e o papel (OP-18, OP-21, OP-23, OP-24 e OP-26).
/// </summary>
/// <param name="UtilizadorId">Identificador do Utilizador.</param>
/// <param name="Nome">Nome da conta.</param>
/// <param name="Email">E-mail da conta.</param>
/// <param name="Papel">Papel na ficha.</param>
/// <param name="Principal">Indica se é o Cuidador principal.</param>
/// <param name="Provisorio">Indica se é Cuidador principal provisório, com um convite para Cuidador principal pendente (RF-025).</param>
public sealed record PessoaAssociada(
    Guid UtilizadorId,
    string Nome,
    string Email,
    Papel Papel,
    bool Principal,
    bool Provisorio);
