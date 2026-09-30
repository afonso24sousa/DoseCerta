// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoseCerta.Api.Tests.Suporte;

/// <summary>
/// Autenticação de teste: o Utilizador é indicado no cabeçalho <c>X-Utilizador</c>, substituindo o JWT da I001.
/// </summary>
public sealed class AutenticacaoTeste : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Nome do esquema de autenticação.
    /// </summary>
    public const string Esquema = "Teste";

    /// <summary>
    /// Cabeçalho com o identificador do Utilizador.
    /// </summary>
    public const string Cabecalho = "X-Utilizador";

    /// <summary>
    /// Initializes a new instance of the <see cref="AutenticacaoTeste"/> class.
    /// </summary>
    /// <param name="options">Opções do esquema.</param>
    /// <param name="logger">Fábrica de registos.</param>
    /// <param name="encoder">Codificador de URLs.</param>
    public AutenticacaoTeste(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.TryGetValue(Cabecalho, out var valor))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identidade = new ClaimsIdentity(new[] { new Claim("sub", valor.ToString()) }, Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}
