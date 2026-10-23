// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Contas;
using DoseCerta.Application.Convites;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Emails;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Pessoas;
using DoseCerta.Application.Tomas;
using DoseCerta.Application.Tratamentos;
using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.Application;

/// <summary>
/// Regista os serviços de aplicação na raiz de composição da API.
/// </summary>
public static class InjecaoDependencias
{
    /// <summary>
    /// Regista os serviços de aplicação, com tempo de vida por pedido.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddAplicacao(this IServiceCollection servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        servicos.AddScoped<AuthService>();
        servicos.AddScoped<ContaService>();
        servicos.AddScoped<AutorizacaoFichaService>();
        servicos.AddScoped<DocumentoService>();
        servicos.AddScoped<FichaService>();
        servicos.AddScoped<PessoasService>();
        servicos.AddScoped<ConviteService>();
        servicos.AddScoped<EnvioEmailService>();
        servicos.AddScoped<TomaService>();
        servicos.AddScoped<TratamentoService>();
        return servicos;
    }
}
