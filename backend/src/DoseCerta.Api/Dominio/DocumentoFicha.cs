// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Documento associado a uma ficha.
/// </summary>
/// <remarks>
/// Só é criado depois de o ficheiro ter sido guardado com sucesso (ver <see cref="ChaveArmazenamento"/>).
/// </remarks>
public class DocumentoFicha
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentoFicha"/> class.
    /// </summary>
    /// <param name="categoria">Categoria do documento.</param>
    /// <param name="nomeOriginal">Nome original do ficheiro.</param>
    /// <param name="tipoConteudo">Tipo MIME do ficheiro.</param>
    /// <param name="tamanhoBytes">Tamanho do ficheiro em bytes.</param>
    /// <param name="chaveArmazenamento">Chave devolvida pelo armazenamento depois de guardar o ficheiro.</param>
    /// <param name="dataCarregamento">Data do carregamento (UTC).</param>
    internal DocumentoFicha(
        CategoriaDocumento categoria,
        string nomeOriginal,
        string tipoConteudo,
        long tamanhoBytes,
        string chaveArmazenamento,
        DateTime dataCarregamento)
    {
        this.Id = Guid.NewGuid();
        this.Categoria = categoria;
        this.NomeOriginal = nomeOriginal;
        this.TipoConteudo = tipoConteudo;
        this.TamanhoBytes = tamanhoBytes;
        this.ChaveArmazenamento = chaveArmazenamento;
        this.DataCarregamento = dataCarregamento;
    }

    /// <summary>
    /// Gets o identificador do documento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid FichaId { get; private set; }

    /// <summary>
    /// Gets a categoria do documento.
    /// </summary>
    public CategoriaDocumento Categoria { get; private set; }

    /// <summary>
    /// Gets o nome original do ficheiro.
    /// </summary>
    public string NomeOriginal { get; private set; }

    /// <summary>
    /// Gets o tipo MIME do ficheiro.
    /// </summary>
    public string TipoConteudo { get; private set; }

    /// <summary>
    /// Gets o tamanho do ficheiro em bytes.
    /// </summary>
    public long TamanhoBytes { get; private set; }

    /// <summary>
    /// Gets a chave do ficheiro no armazenamento.
    /// </summary>
    public string ChaveArmazenamento { get; private set; }

    /// <summary>
    /// Gets a data do carregamento (UTC).
    /// </summary>
    public DateTime DataCarregamento { get; private set; }
}
