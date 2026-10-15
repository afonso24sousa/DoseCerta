// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Contas;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Emissão de tokens JWT assinados, com identificador <c>jti</c> e validade de 8 horas (RNF-009, RNF-054).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Emite um token de sessão para o Utilizador.
    /// </summary>
    /// <param name="utilizador">Utilizador autenticado.</param>
    /// <param name="agoraUtc">Instante de emissão, em UTC.</param>
    /// <returns>O token e os respetivos dados.</returns>
    TokenSessao Emitir(Utilizador utilizador, DateTime agoraUtc);
}
