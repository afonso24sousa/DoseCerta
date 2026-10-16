// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.Extensions.DependencyInjection;

namespace DoseCerta.Application;

/// <summary>
/// Regista os serviços de aplicação na raiz de composição da API.
/// </summary>
public static class InjecaoDependencias
{
    /// <summary>
    /// Regista os serviços de aplicação, com tempo de vida por pedido.
    /// Nesta versão ainda não existem serviços de negócio: o método define o ponto único de registo da camada.
    /// </summary>
    /// <param name="servicos">Coleção de serviços.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddAplicacao(this IServiceCollection servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        return servicos;
    }
}
