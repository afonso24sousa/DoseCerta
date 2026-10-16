// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Erros;
using DoseCerta.Application.Contas;
using DoseCerta.Domain.Comum;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DoseCerta.Api.Seguranca;

/// <summary>
/// Eventos da autenticação JWT: responde 401 <c>NAO_AUTENTICADO</c> a pedidos sem token válido
/// (RF-017, RNF-009) e recusa os tokens de sessões terminadas (RF-016).
/// </summary>
public class ValidadorDeCredencial : JwtBearerEvents
{
    /// <summary>
    /// Mensagem devolvida quando a sessão não é válida.
    /// </summary>
    public const string MensagemNaoAutenticado = "A sessão não é válida ou expirou. Inicie sessão novamente.";

    /// <inheritdoc/>
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Principal is null)
        {
            context.Fail(MensagemNaoAutenticado);
            return;
        }

        var jti = context.Principal.FindFirst("jti")?.Value;
        var servico = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
        if (!Guid.TryParse(jti, out var id)
            || !await servico.CredencialAceiteAsync(id, context.HttpContext.RequestAborted).ConfigureAwait(false))
        {
            context.Fail(MensagemNaoAutenticado);
        }
    }

    /// <inheritdoc/>
    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response
            .WriteAsJsonAsync(new ProblemaApi(CodigosErro.NaoAutenticado, MensagemNaoAutenticado))
            .ConfigureAwait(false);
    }
}
