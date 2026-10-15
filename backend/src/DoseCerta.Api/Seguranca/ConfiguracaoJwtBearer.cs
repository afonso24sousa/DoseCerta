// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text;
using DoseCerta.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoseCerta.Api.Seguranca;

/// <summary>
/// Configura a validação dos tokens JWT a partir de <see cref="OpcoesJwt"/>: assinatura HMAC-SHA256,
/// emissor, público e prazo de validade obrigatório, sem tolerância de relógio (AD-02, RNF-009, RNF-054).
/// </summary>
public sealed class ConfiguracaoJwtBearer : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly OpcoesJwt opcoesJwt;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguracaoJwtBearer"/> class.
    /// </summary>
    /// <param name="opcoesJwt">Configuração dos tokens JWT.</param>
    public ConfiguracaoJwtBearer(IOptions<OpcoesJwt> opcoesJwt)
    {
        ArgumentNullException.ThrowIfNull(opcoesJwt);
        this.opcoesJwt = opcoesJwt.Value;
    }

    /// <inheritdoc/>
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            this.Configure(options);
        }
    }

    /// <inheritdoc/>
    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.opcoesJwt.Validar();

        options.MapInboundClaims = false;
        options.EventsType = typeof(ValidadorDeCredencial);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = this.opcoesJwt.Emissor,
            ValidateAudience = true,
            ValidAudience = this.opcoesJwt.Publico,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.opcoesJwt.Chave)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
        };
    }
}
