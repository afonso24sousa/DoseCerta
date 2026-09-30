// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Armazenamento dos ficheiros dos documentos das fichas.
/// </summary>
public interface IArmazenamentoDocumentos
{
    /// <summary>
    /// Guarda um ficheiro. Só termina com sucesso depois de o ficheiro estar integralmente gravado.
    /// </summary>
    /// <param name="conteudo">Conteúdo do ficheiro.</param>
    /// <param name="nomeFicheiro">Nome original do ficheiro (usado apenas para obter a extensão).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A chave que identifica o ficheiro no armazenamento.</returns>
    Task<string> GuardarAsync(Stream conteudo, string nomeFicheiro, CancellationToken cancellationToken);

    /// <summary>
    /// Remove um ficheiro. Não falha se o ficheiro já não existir.
    /// </summary>
    /// <param name="chave">Chave devolvida por <see cref="GuardarAsync"/>.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task RemoverAsync(string chave, CancellationToken cancellationToken);
}
