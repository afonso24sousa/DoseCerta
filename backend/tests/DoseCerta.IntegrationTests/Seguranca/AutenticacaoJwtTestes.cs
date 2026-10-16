// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Security.Claims;
using System.Text;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DoseCerta.IntegrationTests.Seguranca;

/// <summary>
/// Testes de integração da autenticação JWT configurada na API (AD-02, RNF-054): os parâmetros de validação
/// são obtidos da API arrancada, com a chave lida da configuração não versionada.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class AutenticacaoJwtTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutenticacaoJwtTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public AutenticacaoJwtTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// Um token assinado com a chave configurada, do emissor e público certos e dentro da validade, é aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TokenValidoEAceite()
    {
        var token = CriarToken(FabricaApi.ChaveJwtTestes, "DoseCerta", DateTime.UtcNow.AddHours(1));

        var resultado = await this.ValidarAsync(token);

        Assert.True(resultado.IsValid);
        Assert.Equal("utilizador-de-teste", resultado.ClaimsIdentity.Name);
    }

    /// <summary>
    /// Um token assinado com outra chave, de outro emissor ou já expirado é recusado.
    /// </summary>
    /// <param name="chave">Chave de assinatura do token.</param>
    /// <param name="emissor">Emissor do token.</param>
    /// <param name="horasAteExpirar">Horas até à expiração (negativo: já expirou).</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("outra-chave-de-testes-com-32-ou-mais-carateres", "DoseCerta", 1)]
    [InlineData(FabricaApi.ChaveJwtTestes, "OutroEmissor", 1)]
    [InlineData(FabricaApi.ChaveJwtTestes, "DoseCerta", -1)]
    public async Task TokenInvalidoERecusado(string chave, string emissor, int horasAteExpirar)
    {
        var token = CriarToken(chave, emissor, DateTime.UtcNow.AddHours(horasAteExpirar));

        var resultado = await this.ValidarAsync(token);

        Assert.False(resultado.IsValid);
    }

    private static string CriarToken(string chave, string emissor, DateTime expiraEmUtc)
    {
        var descritor = new SecurityTokenDescriptor
        {
            Issuer = emissor,
            Audience = "DoseCerta",
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "utilizador-de-teste") }),
            NotBefore = expiraEmUtc.AddHours(-2),
            IssuedAt = expiraEmUtc.AddHours(-2),
            Expires = expiraEmUtc,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descritor);
    }

    private Task<TokenValidationResult> ValidarAsync(string token)
    {
        var opcoes = this.fabrica.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        return new JsonWebTokenHandler().ValidateTokenAsync(token, opcoes.TokenValidationParameters);
    }
}
