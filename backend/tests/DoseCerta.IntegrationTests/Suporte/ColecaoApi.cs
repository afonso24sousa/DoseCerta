// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Coleção xUnit que partilha a mesma <see cref="FabricaApi"/> (e a mesma base de dados de testes)
/// entre as classes de teste, para não recriar a base de dados em cada classe.
/// </summary>
[CollectionDefinition(Nome)]
public class ColecaoApi : ICollectionFixture<FabricaApi>
{
    /// <summary>
    /// Nome da coleção.
    /// </summary>
    public const string Nome = "API com SQL Server";
}
