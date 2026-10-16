// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Contas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoseCerta.Infrastructure.Seguranca;

/// <summary>
/// Emissão de tokens JWT assinados com HMAC-SHA256, com <c>sub</c> (Utilizador), <c>jti</c> (credencial)
/// e validade de 8 horas (AD-02, RNF-009, RNF-054). Os tokens não são renovados.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly OpcoesJwt opcoes;
    private readonly JwtSecurityTokenHandler manipulador = new() { MapInboundClaims = false };

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="opcoes">Configuração dos tokens JWT.</param>
    public JwtTokenService(IOptions<OpcoesJwt> opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        this.opcoes = opcoes.Value;
    }

    /// <inheritdoc/>
    public TokenSessao Emitir(Utilizador utilizador, DateTime agoraUtc)
    {
        ArgumentNullException.ThrowIfNull(utilizador);
        this.opcoes.Validar();

        var jti = Guid.NewGuid();
        var emitidoEm = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);
        var expiraEm = emitidoEm.Add(ParametrosDeNegocio.ValidadeSessao);
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.opcoes.Chave)),
            SecurityAlgorithms.HmacSha256);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = this.opcoes.Emissor,
            Audience = this.opcoes.Publico,
            IssuedAt = emitidoEm,
            NotBefore = emitidoEm,
            Expires = expiraEm,
            SigningCredentials = credenciais,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, utilizador.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            }),
        };

        var token = this.manipulador.WriteToken(this.manipulador.CreateToken(descritor));
        return new TokenSessao(token, jti, expiraEm);
    }
}
