// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos metadados dos documentos (DocumentoRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface IDocumentoRepositorio
{
    /// <summary>
    /// Obtém um documento.
    /// </summary>
    /// <param name="id">Identificador do documento.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O documento, ou <see langword="null"/> se não existir.</returns>
    Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Lista os documentos de uma ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os documentos, do mais recente para o mais antigo.</returns>
    Task<IReadOnlyList<Documento>> DaFichaAsync(Guid fichaId, CancellationToken cancelamento);

    /// <summary>
    /// Regista os metadados de um documento cujo ficheiro já foi guardado (RNF-046).
    /// </summary>
    /// <param name="documento">Documento a registar.</param>
    void Adicionar(Documento documento);
}
