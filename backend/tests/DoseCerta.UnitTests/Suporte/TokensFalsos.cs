// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Contas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Emissor de tokens falso para os testes unitários dos serviços.
/// </summary>
public sealed class TokensFalsos : ITokenService
{
    /// <inheritdoc/>
    public TokenSessao Emitir(Utilizador utilizador, DateTime agoraUtc)
    {
        ArgumentNullException.ThrowIfNull(utilizador);
        return new TokenSessao("token-" + utilizador.Id, Guid.NewGuid(), agoraUtc.Add(ParametrosDeNegocio.ValidadeSessao));
    }
}
