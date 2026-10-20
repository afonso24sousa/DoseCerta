// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Seguranca;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários do hash das palavras-passe (RNF-001) e da emissão dos tokens JWT (RNF-009, RNF-054).
/// </summary>
public class SegurancaTestes
{
    private const string Chave = "chave-de-testes-unitarios-dosecerta-com-32-carateres";

    /// <summary>
    /// O hash tem salt aleatório: a mesma palavra-passe dá hashes diferentes, ambos verificáveis.
    /// </summary>
    [Fact]
    public void HashTemSaltAleatorioEEVerificavel()
    {
        var hasher = new PasswordHasherPbkdf2();

        var primeiro = hasher.Hash("Segura123");
        var segundo = hasher.Hash("Segura123");

        Assert.NotEqual(primeiro, segundo);
        Assert.StartsWith("PBKDF2-SHA256$100000$", primeiro, StringComparison.Ordinal);
        Assert.DoesNotContain("Segura123", primeiro, StringComparison.Ordinal);
        Assert.True(hasher.Verificar(primeiro, "Segura123"));
        Assert.True(hasher.Verificar(segundo, "Segura123"));
        Assert.False(hasher.Verificar(primeiro, "Errada123"));
    }

    /// <summary>
    /// Um hash com formato inválido nunca é aceite.
    /// </summary>
    /// <param name="hash">Hash inválido.</param>
    [Theory]
    [InlineData("")]
    [InlineData("texto-simples")]
    [InlineData("MD5$1$abc$def")]
    [InlineData("PBKDF2-SHA256$x$abc$def")]
    [InlineData("PBKDF2-SHA256$1000$###$###")]
    public void HashComFormatoInvalidoNaoEAceite(string hash)
    {
        Assert.False(new PasswordHasherPbkdf2().Verificar(hash, "Segura123"));
    }

    /// <summary>
    /// As contas de demonstração do script de dados iniciais usam o mesmo formato de hash (I030).
    /// </summary>
    [Fact]
    public void HashDoScriptDeDadosIniciaisEAceite()
    {
        const string HashAna = "PBKDF2-SHA256$100000$Ea0oZw/bbnMr2HlANteVpw==$HAPgoUz0LOBxLJfHew2dG1zurKaZvCIgIlhk28N7yBI=";

        Assert.True(new PasswordHasherPbkdf2().Verificar(HashAna, "Demo2026x"));
    }

    /// <summary>
    /// O token tem sub, jti, emissor e público configurados e expira 8 horas depois da emissão.
    /// </summary>
    [Fact]
    public void TokenTemOsDadosEAValidadeDeOitoHoras()
    {
        var servico = new JwtTokenService(Options.Create(new OpcoesJwt { Chave = Chave }));
        var utilizador = new Utilizador("Ana", "ana@exemplo.pt", "hash", DateTime.UtcNow);
        var agora = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);

        var sessao = servico.Emitir(utilizador, agora);
        var token = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(sessao.Token);

        Assert.Equal(agora.AddHours(8), sessao.ExpiraEmUtc);
        Assert.Equal(sessao.ExpiraEmUtc, token.ValidTo);
        Assert.Equal(utilizador.Id.ToString(), token.Subject);
        Assert.Equal(sessao.Jti.ToString(), token.Id);
        Assert.Equal("DoseCerta", token.Issuer);
        Assert.Contains("DoseCerta", token.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
    }

    /// <summary>
    /// O token só é válido com a chave do servidor (assinatura alterada é recusada).
    /// </summary>
    [Fact]
    public void TokenComOutraChaveERecusado()
    {
        var servico = new JwtTokenService(Options.Create(new OpcoesJwt { Chave = Chave }));
        var sessao = servico.Emitir(new Utilizador("Ana", "ana@exemplo.pt", "hash", DateTime.UtcNow), DateTime.UtcNow);
        var parametros = new TokenValidationParameters
        {
            ValidIssuer = "DoseCerta",
            ValidAudience = "DoseCerta",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave.Replace('a', 'b'))),
        };

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(sessao.Token, parametros, out _));
    }
}
