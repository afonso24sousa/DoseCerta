// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dados;
using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Api.Infraestrutura;

/// <summary>
/// Preparação da base de dados local em ambiente de desenvolvimento.
/// </summary>
/// <remarks>
/// Enquanto a I001 não disponibiliza o registo e o login, é criado um Utilizador fictício de teste,
/// usado com um token gerado por <c>dotnet user-jwts</c> (ver README do backend).
/// </remarks>
public static class DadosDesenvolvimento
{
    /// <summary>
    /// Identificador do Utilizador fictício de teste.
    /// </summary>
    public static readonly Guid UtilizadorTesteId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Aplica as migrações pendentes e cria o Utilizador de teste, se ainda não existir.
    /// </summary>
    /// <param name="servicos">Fornecedor de serviços da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    public static async Task PrepararAsync(IServiceProvider servicos, CancellationToken cancellationToken = default)
    {
        await using var ambito = servicos.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<DoseCertaDbContext>();

        await contexto.Database.MigrateAsync(cancellationToken);

        if (!await contexto.Utilizadores.AnyAsync(u => u.Id == UtilizadorTesteId, cancellationToken))
        {
            contexto.Utilizadores.Add(new Utilizador
            {
                Id = UtilizadorTesteId,
                Nome = "Utilizador de Teste",
                Email = "teste@dosecerta.local",
            });
            await contexto.SaveChangesAsync(cancellationToken);
        }
    }
}
