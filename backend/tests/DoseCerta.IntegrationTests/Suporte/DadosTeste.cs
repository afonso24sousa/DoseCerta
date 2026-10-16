// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Infrastructure.Persistence;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Criação de dados fictícios para os testes de integração (RNF-050). Cada chamada usa e-mails únicos,
/// para que os testes não dependam uns dos outros na base de dados partilhada.
/// </summary>
public static class DadosTeste
{
    /// <summary>
    /// Obtém um instante UTC com precisão de segundos, compatível com <c>datetime2(0)</c>.
    /// </summary>
    /// <returns>O instante atual arredondado ao segundo.</returns>
    public static DateTime Agora()
    {
        var agora = DateTime.UtcNow;
        return new DateTime(agora.Ticks - (agora.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    /// <summary>
    /// Gera um e-mail fictício único.
    /// </summary>
    /// <param name="prefixo">Prefixo do endereço.</param>
    /// <returns>O e-mail gerado.</returns>
    public static string EmailUnico(string prefixo = "teste")
    {
        return $"{prefixo}.{Guid.NewGuid():N}@exemplo.pt";
    }

    /// <summary>
    /// Cria e grava um Utilizador fictício.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    /// <param name="email">E-mail; se omitido, é gerado.</param>
    /// <returns>O Utilizador criado.</returns>
    public static async Task<Utilizador> CriarUtilizadorAsync(DoseCertaDbContext contexto, string? email = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var utilizador = new Utilizador("Pessoa Fictícia", email ?? EmailUnico(), "hash-ficticio", Agora());
        contexto.Utilizadores.Add(utilizador);
        await contexto.SaveChangesAsync();
        return utilizador;
    }

    /// <summary>
    /// Cria e grava uma ficha fictícia.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    /// <param name="criadorId">Utilizador que cria a ficha.</param>
    /// <param name="propria">Indica se é a ficha própria do criador.</param>
    /// <returns>A ficha criada.</returns>
    public static async Task<Ficha> CriarFichaAsync(DoseCertaDbContext contexto, Guid criadorId, bool propria = false)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var ficha = new Ficha(
            "Utente Fictício",
            new DateOnly(1941, 5, 17),
            "+351 910 000 001",
            propria ? criadorId : null,
            criadorId,
            Agora());
        contexto.Fichas.Add(ficha);
        await contexto.SaveChangesAsync();
        return ficha;
    }
}
