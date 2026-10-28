// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Infrastructure.Documentos;
using DoseCerta.Infrastructure.Email;
using DoseCerta.Infrastructure.Persistence;
using DoseCerta.Infrastructure.Persistence.Repositorios;
using DoseCerta.Infrastructure.Seguranca;
using DoseCerta.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.Infrastructure;

/// <summary>
/// Regista a infraestrutura na raiz de composição da API.
/// </summary>
public static class InjecaoDependencias
{
    /// <summary>
    /// Nome da connection string do SQL Server na configuração.
    /// </summary>
    public const string NomeConnectionString = "DoseCerta";

    /// <summary>
    /// Regista o contexto EF Core, a unidade de trabalho, o relógio, o envio de e-mail e as opções JWT.
    /// A connection string vem de configuração local não versionada (user-secrets ou variável de ambiente).
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <param name="configuracao">Configuração da aplicação.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        servicos.AddDbContext<DoseCertaDbContext>(opcoes =>
            opcoes.UseSqlServer(configuracao.GetConnectionString(NomeConnectionString)));

        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<IRelogio, RelogioSistema>();

        servicos.Configure<OpcoesEmail>(configuracao.GetSection(OpcoesEmail.Seccao));
        servicos.AddScoped<IEmailSender, EmailSenderSmtp>();

        servicos.Configure<OpcoesJwt>(configuracao.GetSection(OpcoesJwt.Seccao));
        servicos.AddSingleton<IPasswordHasher, PasswordHasherPbkdf2>();
        servicos.AddSingleton<ITokenService, JwtTokenService>();

        servicos.AddScoped<IUtilizadorRepositorio, UtilizadorRepositorio>();
        servicos.AddScoped<ISessaoRevogadaRepositorio, SessaoRevogadaRepositorio>();
        servicos.AddScoped<IConviteRepositorio, ConviteRepositorio>();
        servicos.AddScoped<IFichaRepositorio, FichaRepositorio>();
        servicos.AddScoped<IDominiosRepositorio, DominiosRepositorio>();
        servicos.AddScoped<IDocumentoRepositorio, DocumentoRepositorio>();
        servicos.AddScoped<INotificacaoRepositorio, NotificacaoRepositorio>();
        servicos.AddScoped<IEnvioEmailRepositorio, EnvioEmailRepositorio>();
        servicos.AddScoped<ITratamentoRepositorio, TratamentoRepositorio>();
        servicos.AddScoped<ITomaRepositorio, TomaRepositorio>();
        servicos.AddScoped<ICompromissoRepositorio, CompromissoRepositorio>();
        servicos.AddScoped<IOcorrenciaRepositorio, OcorrenciaRepositorio>();
        servicos.AddScoped<INotaRepositorio, NotaRepositorio>();

        servicos.Configure<OpcoesDocumentos>(configuracao.GetSection(OpcoesDocumentos.Seccao));
        servicos.AddSingleton<IArmazenamentoDocumentos, ArmazenamentoSistemaFicheiros>();

        return servicos;
    }
}
