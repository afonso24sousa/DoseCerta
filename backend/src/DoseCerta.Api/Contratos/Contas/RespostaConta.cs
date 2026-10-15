// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Dados da conta (OP-04 e OP-05); nunca inclui a palavra-passe nem o hash (RF-013).
/// </summary>
/// <param name="Id">Identificador da conta.</param>
/// <param name="Nome">Nome da pessoa.</param>
/// <param name="Email">E-mail da conta.</param>
public sealed record RespostaConta(
    Guid Id,
    string Nome,
    string Email);
