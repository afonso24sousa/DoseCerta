// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Arranque;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Infrastructure.Email;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.Infrastructure.Seguranca;
using DoseCerta.Infrastructure.Tempo;
using DoseCerta.UnitTests.Suporte;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários da configuração da solução (JWT, e-mail, relógio, contexto EF Core e comando de teste do e-mail).
/// </summary>
public class ConfiguracaoTestes
{
    private const string ChaveValida = "chave-de-testes-unitarios-com-32-ou-mais-carateres";

    /// <summary>
    /// Uma chave JWT em falta ou curta é recusada com uma mensagem clara.
    /// </summary>
    /// <param name="chave">Chave inválida.</param>
    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    public void OpcoesJwtRecusaChaveInvalida(string chave)
    {
        var opcoes = new OpcoesJwt { Chave = chave };

        var erro = Assert.Throws<InvalidOperationException>(opcoes.Validar);
        Assert.Contains("Jwt__Chave", erro.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Uma chave JWT com 32 carateres é aceite.
    /// </summary>
    [Fact]
    public void OpcoesJwtAceitaChaveComTamanhoMinimo()
    {
        var opcoes = new OpcoesJwt { Chave = new string('k', OpcoesJwt.TamanhoMinimoChave) };

        opcoes.Validar();

        Assert.Equal(OpcoesJwt.TamanhoMinimoChave + 1, opcoes.Chave.Length);
    }

    /// <summary>
    /// A validação dos tokens exige assinatura HMAC-SHA256 com a chave configurada, emissor, público
    /// e prazo de validade, sem tolerância de relógio.
    /// </summary>
    [Fact]
    public void ConfiguracaoJwtBearerDefineAValidacaoDoToken()
    {
        var configuracao = new ConfiguracaoJwtBearer(Options.Create(new OpcoesJwt { Chave = ChaveValida }));
        var opcoes = new JwtBearerOptions();

        configuracao.Configure(JwtBearerDefaults.AuthenticationScheme, opcoes);

        var parametros = opcoes.TokenValidationParameters;
        Assert.False(opcoes.MapInboundClaims);
        Assert.True(parametros.ValidateIssuer);
        Assert.Equal("DoseCerta", parametros.ValidIssuer);
        Assert.True(parametros.ValidateAudience);
        Assert.Equal("DoseCerta", parametros.ValidAudience);
        Assert.True(parametros.ValidateIssuerSigningKey);
        Assert.IsType<SymmetricSecurityKey>(parametros.IssuerSigningKey);
        Assert.Equal(new[] { SecurityAlgorithms.HmacSha256 }, parametros.ValidAlgorithms);
        Assert.True(parametros.ValidateLifetime);
        Assert.True(parametros.RequireExpirationTime);
        Assert.Equal(TimeSpan.Zero, parametros.ClockSkew);
        Assert.Equal("sub", parametros.NameClaimType);
    }

    /// <summary>
    /// Outros esquemas de autenticação não são alterados.
    /// </summary>
    [Fact]
    public void ConfiguracaoJwtBearerIgnoraOutrosEsquemas()
    {
        var configuracao = new ConfiguracaoJwtBearer(Options.Create(new OpcoesJwt { Chave = ChaveValida }));
        var opcoes = new JwtBearerOptions();

        configuracao.Configure("OutroEsquema", opcoes);

        Assert.Null(opcoes.TokenValidationParameters.ValidIssuer);
    }

    /// <summary>
    /// Sem chave JWT configurada, a autenticação não arranca e indica como definir a chave.
    /// </summary>
    [Fact]
    public void ConfiguracaoJwtBearerSemChaveFalha()
    {
        var configuracao = new ConfiguracaoJwtBearer(Options.Create(new OpcoesJwt()));

        Assert.Throws<InvalidOperationException>(() => configuracao.Configure(new JwtBearerOptions()));
    }

    /// <summary>
    /// O e-mail só é considerado configurado com servidor e remetente.
    /// </summary>
    [Fact]
    public void OpcoesEmailExigemServidorERemetente()
    {
        Assert.False(new OpcoesEmail().Configurado);
        Assert.False(new OpcoesEmail { Servidor = "smtp.exemplo.pt" }.Configurado);
        Assert.True(new OpcoesEmail { Servidor = "smtp.exemplo.pt", Remetente = "dosecerta@exemplo.pt" }.Configurado);
    }

    /// <summary>
    /// Sem configuração SMTP, o envio não é tentado e devolve falso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailSenderSmtpSemConfiguracaoDevolveFalso()
    {
        var envio = new EmailSenderSmtp(Options.Create(new OpcoesEmail()), NullLogger<EmailSenderSmtp>.Instance);

        var aceite = await envio.EnviarAsync("ana@exemplo.pt", "Assunto", "Corpo", CancellationToken.None);

        Assert.False(aceite);
    }

    /// <summary>
    /// Uma falha na ligação ao servidor SMTP não é propagada: o envio devolve falso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailSenderSmtpComServidorIndisponivelDevolveFalso()
    {
        var opcoes = new OpcoesEmail
        {
            Servidor = "127.0.0.1",
            Porta = 1,
            UsarTls = false,
            Utilizador = "utilizador",
            PalavraPasse = "palavra-passe-ficticia",
            Remetente = "dosecerta@exemplo.pt",
        };
        var envio = new EmailSenderSmtp(Options.Create(opcoes), NullLogger<EmailSenderSmtp>.Instance);

        var aceite = await envio.EnviarAsync("ana@exemplo.pt", "Assunto", "Corpo", CancellationToken.None);

        Assert.False(aceite);
    }

    /// <summary>
    /// O comando do e-mail de teste só é ativado com o argumento e um destino.
    /// </summary>
    [Fact]
    public void ComandoEmailTesteReconheceOArgumento()
    {
        Assert.True(ComandoEmailTeste.Pedido(new[] { ComandoEmailTeste.Argumento, "ana@exemplo.pt" }, out var destino));
        Assert.Equal("ana@exemplo.pt", destino);
        Assert.False(ComandoEmailTeste.Pedido(new[] { ComandoEmailTeste.Argumento }, out _));
        Assert.False(ComandoEmailTeste.Pedido(Array.Empty<string>(), out _));
    }

    /// <summary>
    /// O comando envia a mensagem de teste ao destino, com a hora de Lisboa, e termina com o código 0.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ComandoEmailTesteEnviaAMensagemEDevolveZero()
    {
        var email = new EmailSenderFalso();
        await using var servicos = CriarServicos(email);

        var codigo = await ComandoEmailTeste.ExecutarAsync(servicos, "ana@exemplo.pt");

        Assert.Equal(0, codigo);
        var (destino, assunto, corpo) = Assert.Single(email.Enviados);
        Assert.Equal("ana@exemplo.pt", destino);
        Assert.Equal("DoseCerta: mensagem de teste", assunto);
        Assert.Contains("07/10/2026 08:05", corpo, StringComparison.Ordinal);
    }

    /// <summary>
    /// Se o serviço de e-mail recusar a mensagem, o comando termina com o código 1.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ComandoEmailTesteComFalhaDevolveUm()
    {
        var email = new EmailSenderFalso { Falhar = true };
        await using var servicos = CriarServicos(email);

        var codigo = await ComandoEmailTeste.ExecutarAsync(servicos, "ana@exemplo.pt");

        Assert.Equal(1, codigo);
        Assert.Empty(email.Enviados);
    }

    /// <summary>
    /// O relógio do sistema devolve o instante em UTC com precisão de um segundo e o calendário de Lisboa.
    /// </summary>
    [Fact]
    public void RelogioSistemaDevolveUtcAoSegundo()
    {
        var relogio = new RelogioSistema(TimeProvider.System);

        var agora = relogio.AgoraUtc();

        Assert.Equal(DateTimeKind.Utc, agora.Kind);
        Assert.Equal(0, agora.Ticks % TimeSpan.TicksPerSecond);
        Assert.Same(relogio.Calendario(), relogio.Calendario());
    }

    /// <summary>
    /// A fábrica usada pelas ferramentas do EF Core cria o contexto sobre SQL Server sem ligar à base de dados.
    /// </summary>
    [Fact]
    public void FabricaDesignCriaContextoSqlServer()
    {
        using var contexto = new FabricaDesignDoseCertaDbContext().CreateDbContext(Array.Empty<string>());

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", contexto.Database.ProviderName);
        Assert.Equal("dbo", contexto.Model.GetDefaultSchema());
    }

    private static ServiceProvider CriarServicos(IEmailSender email)
    {
        return new ServiceCollection()
            .AddSingleton(email)
            .AddSingleton<IRelogio>(new RelogioFixo(new DateTime(2026, 10, 7, 7, 5, 0, DateTimeKind.Utc)))
            .BuildServiceProvider();
    }
}
