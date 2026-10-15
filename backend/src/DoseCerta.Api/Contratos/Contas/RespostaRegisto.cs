// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Resposta da criação de conta (OP-01).
/// </summary>
/// <param name="Id">Identificador da conta.</param>
/// <param name="Nome">Nome da pessoa.</param>
/// <param name="Email">E-mail da conta.</param>
/// <param name="ConvitesPendentes">Número de convites pendentes para o e-mail (RF-044).</param>
public sealed record RespostaRegisto(
    Guid Id,
    string Nome,
    string Email,
    int ConvitesPendentes);
