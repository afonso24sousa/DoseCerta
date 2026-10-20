// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Resposta do início de sessão (OP-02).
/// </summary>
/// <param name="Token">Token JWT assinado.</param>
/// <param name="Tipo">Tipo do token, sempre Bearer.</param>
/// <param name="ExpiraEm">Instante de expiração, com o desvio de Lisboa.</param>
public sealed record RespostaSessao(
    string Token,
    string Tipo,
    DateTimeOffset ExpiraEm);
