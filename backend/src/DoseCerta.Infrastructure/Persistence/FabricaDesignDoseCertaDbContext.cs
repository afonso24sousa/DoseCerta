// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada apenas pelas ferramentas do EF Core (<c>dotnet ef</c>) para gerar migrações e scripts,
/// sem arrancar a API. A connection string só é usada pelos comandos que ligam à base de dados
/// e lê-se da variável de ambiente <c>ConnectionStrings__DoseCerta</c>.
/// </summary>
public sealed class FabricaDesignDoseCertaDbContext : IDesignTimeDbContextFactory<DoseCertaDbContext>
{
    private const string LigacaoPorOmissao =
        "Server=localhost,1433;Database=DoseCerta;Integrated Security=false;TrustServerCertificate=true";

    /// <inheritdoc/>
    public DoseCertaDbContext CreateDbContext(string[] args)
    {
        var ligacao = Environment.GetEnvironmentVariable("ConnectionStrings__DoseCerta");
        var opcoes = new DbContextOptionsBuilder<DoseCertaDbContext>()
            .UseSqlServer(string.IsNullOrWhiteSpace(ligacao) ? LigacaoPorOmissao : ligacao)
            .Options;
        return new DoseCertaDbContext(opcoes);
    }
}
