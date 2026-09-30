// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dados;
using DoseCerta.Api.Servicos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DoseCerta.Api.Tests.Suporte;

/// <summary>
/// Arranca a API em memória sobre SQLite, com armazenamento em memória e autenticação de teste.
/// </summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    /// <summary>
    /// Gets a base de dados usada pela API.
    /// </summary>
    public BaseDadosTeste BaseDados { get; } = new();

    /// <summary>
    /// Gets o armazenamento de documentos usado pela API.
    /// </summary>
    public ArmazenamentoEmMemoria Armazenamento { get; } = new();

    /// <summary>
    /// Cria um cliente HTTP autenticado como o Utilizador indicado.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <returns>O cliente.</returns>
    public HttpClient CriarClienteComo(Guid utilizadorId)
    {
        var cliente = this.CreateClient();
        cliente.DefaultRequestHeaders.Add(AutenticacaoTeste.Cabecalho, utilizadorId.ToString());
        return cliente;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testes");
        builder.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<DoseCertaDbContext>>();
            servicos.AddDbContext<DoseCertaDbContext>(o => o.UseSqlite(this.BaseDados.Ligacao));

            servicos.RemoveAll<IArmazenamentoDocumentos>();
            servicos.AddSingleton<IArmazenamentoDocumentos>(this.Armazenamento);

            servicos.AddAuthentication(AutenticacaoTeste.Esquema)
                .AddScheme<AuthenticationSchemeOptions, AutenticacaoTeste>(AutenticacaoTeste.Esquema, null);
            servicos.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = AutenticacaoTeste.Esquema;
                o.DefaultAuthenticateScheme = AutenticacaoTeste.Esquema;
                o.DefaultChallengeScheme = AutenticacaoTeste.Esquema;
            });
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.BaseDados.Dispose();
        }
    }
}
