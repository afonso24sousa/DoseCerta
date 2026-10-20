// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Hash determinístico e rápido para os testes unitários dos serviços (o algoritmo real é testado à parte).
/// </summary>
public sealed class HasherSimples : IPasswordHasher
{
    /// <inheritdoc/>
    public string Hash(string palavraPasse)
    {
        return "hash:" + palavraPasse;
    }

    /// <inheritdoc/>
    public bool Verificar(string hash, string palavraPasse)
    {
        return string.Equals(hash, "hash:" + palavraPasse, StringComparison.Ordinal);
    }
}
