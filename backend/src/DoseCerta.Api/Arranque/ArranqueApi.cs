// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json;
using System.Text.Json.Serialization;
using DoseCerta.Api.Erros;
using DoseCerta.Api.Processos;
using DoseCerta.Api.Saude;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application;
using DoseCerta.Infrastructure;
using DoseCerta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

namespace DoseCerta.Api.Arranque;

/// <summary>
/// Raiz de composição da API: regista os serviços, a infraestrutura e a autenticação JWT
/// e configura o pipeline HTTP (Modelo de Classes do Backend, classe ArranqueApi).
/// </summary>
public static class ArranqueApi
{
    /// <summary>
    /// Caminho do endpoint de verificação do estado da aplicação.
    /// </summary>
    public const string CaminhoSaude = "/saude";

    /// <summary>
    /// Regista os serviços da API.
    /// </summary>
    /// <param name="builder">Construtor da aplicação.</param>
    public static void ConfigurarServicos(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var servicos = builder.Services;

        servicos.AddAplicacao();
        servicos.AddInfraestrutura(builder.Configuration);

        // Processo periódico (passo 7: envio e reenvios dos e-mails de convite, RF-136).
        servicos.Configure<OpcoesProcessoPeriodico>(builder.Configuration.GetSection(OpcoesProcessoPeriodico.Seccao));
        servicos.AddHostedService<ProcessoPeriodico>();

        // Autenticação JWT e exigência de sessão em todos os endpoints, exceto os marcados como públicos (RF-017).
        servicos.ConfigureOptions<ConfiguracaoJwtBearer>();
        servicos.AddScoped<ValidadorDeCredencial>();
        servicos.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        servicos.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        servicos.AddExceptionHandler<FiltroDeErros>();
        servicos.AddProblemDetails();
        servicos.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        servicos.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = RespostaValidacaoModelo.Criar);
        servicos.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        servicos.AddHealthChecks().AddCheck<VerificacaoBaseDados>("baseDados");

        // Registo dos pedidos sem corpos nem cabeçalhos, para nunca escrever palavras-passe ou tokens no log (RNF-001).
        servicos.AddHttpLogging(o => o.LoggingFields =
            HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
            | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration);

        servicos.AddEndpointsApiExplorer();
        servicos.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "DoseCerta API", Version = "v1" });
            var esquema = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT da sessão, enviado no cabeçalho Authorization.",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            o.AddSecurityDefinition("Bearer", esquema);
            o.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
        });
    }

    /// <summary>
    /// Configura o pipeline HTTP e os endpoints.
    /// </summary>
    /// <param name="app">Aplicação.</param>
    public static void ConfigurarPipeline(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseHttpLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        // Todos os pedidos por HTTPS; os pedidos HTTP são redirecionados (RNF-002).
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks(CaminhoSaude, new HealthCheckOptions { ResponseWriter = EscreverSaudeAsync })
            .AllowAnonymous();
    }

    /// <summary>
    /// Aplica as migrações pendentes do EF Core à base de dados configurada (RNF-057).
    /// </summary>
    /// <param name="app">Aplicação.</param>
    /// <returns>Uma tarefa que termina quando as migrações estiverem aplicadas.</returns>
    public static async Task AplicarMigracoesAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        await using var ambito = app.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();
        await contexto.Database.MigrateAsync().ConfigureAwait(false);
    }

    private static Task EscreverSaudeAsync(HttpContext contexto, HealthReport relatorio)
    {
        var corpo = new
        {
            estado = relatorio.Status == HealthStatus.Healthy ? "Saudavel" : "Indisponivel",
            verificacoes = relatorio.Entries.ToDictionary(
                e => e.Key,
                e => e.Value.Status == HealthStatus.Healthy ? "Saudavel" : "Indisponivel"),
        };
        return contexto.Response.WriteAsJsonAsync(corpo);
    }
}
