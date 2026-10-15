// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Contas;

/// <summary>
/// Token JWT emitido no início de sessão.
/// </summary>
/// <param name="Token">Token JWT assinado.</param>
/// <param name="Jti">Identificador da credencial.</param>
/// <param name="ExpiraEmUtc">Instante de expiração, em UTC.</param>
public sealed record TokenSessao(string Token, Guid Jti, DateTime ExpiraEmUtc);
