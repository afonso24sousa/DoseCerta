// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Cálculo e verificação do hash com salt das palavras-passe (RNF-001).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Calcula o hash com salt aleatório de uma palavra-passe.
    /// </summary>
    /// <param name="palavraPasse">Palavra-passe em texto.</param>
    /// <returns>O hash, com o algoritmo, as iterações e o salt.</returns>
    string Hash(string palavraPasse);

    /// <summary>
    /// Verifica se uma palavra-passe corresponde a um hash.
    /// </summary>
    /// <param name="hash">Hash guardado.</param>
    /// <param name="palavraPasse">Palavra-passe em texto.</param>
    /// <returns><see langword="true"/> se a palavra-passe corresponder ao hash.</returns>
    bool Verificar(string hash, string palavraPasse);
}
