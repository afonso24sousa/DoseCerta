// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Guarda, lê e elimina os ficheiros dos documentos no sistema de ficheiros do servidor, sem endereço público
/// (RNF-006, RNF-007, AD-06).
/// </summary>
public interface IArmazenamentoDocumentos
{
    /// <summary>
    /// Guarda o conteúdo de um ficheiro com um nome gerado pelo servidor.
    /// </summary>
    /// <param name="conteudo">Conteúdo do ficheiro.</param>
    /// <param name="formato">Formato do ficheiro, que define a extensão.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O caminho relativo do ficheiro guardado.</returns>
    /// <exception cref="IOException">Se o ficheiro não puder ser guardado.</exception>
    Task<string> GuardarAsync(Stream conteudo, FormatoDocumento formato, CancellationToken cancelamento);

    /// <summary>
    /// Abre um ficheiro guardado para leitura.
    /// </summary>
    /// <param name="caminhoRelativo">Caminho relativo devolvido por <see cref="GuardarAsync"/>.</param>
    /// <returns>O conteúdo do ficheiro.</returns>
    /// <exception cref="IOException">Se o ficheiro não puder ser lido.</exception>
    Stream Abrir(string caminhoRelativo);

    /// <summary>
    /// Elimina um ficheiro guardado. Repete a remoção quando falha e regista a falha final, sem lançar exceção,
    /// para não ficarem ficheiros órfãos nem desfazer a operação que a pediu (RNF-046).
    /// </summary>
    /// <param name="caminhoRelativo">Caminho relativo do ficheiro.</param>
    void Eliminar(string caminhoRelativo);
}
