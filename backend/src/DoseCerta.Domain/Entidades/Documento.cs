// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Metadados de um documento guardado no sistema de ficheiros do servidor, com a categoria e, na receita, o tratamento (RF-022, RF-052, DM-10).
/// </summary>
public class Documento
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Documento"/> class.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="tratamentoId">Identificador do tratamento, só para receitas.</param>
    /// <param name="categoria">Categoria do documento.</param>
    /// <param name="nomeOriginal">Nome original do ficheiro.</param>
    /// <param name="caminhoRelativo">Caminho interno do ficheiro.</param>
    /// <param name="formato">Formato do ficheiro.</param>
    /// <param name="tamanhoBytes">Tamanho em bytes.</param>
    /// <param name="carregadoPorId">Utilizador que carregou.</param>
    /// <param name="carregadoEmUtc">Instante do carregamento, em UTC.</param>
    public Documento(
        Guid fichaId,
        Guid? tratamentoId,
        CategoriaDocumento categoria,
        string nomeOriginal,
        string caminhoRelativo,
        FormatoDocumento formato,
        int tamanhoBytes,
        Guid carregadoPorId,
        DateTime carregadoEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.FichaId = fichaId;
        this.TratamentoId = tratamentoId;
        this.Categoria = categoria;
        this.NomeOriginal = nomeOriginal;
        this.CaminhoRelativo = caminhoRelativo;
        this.Formato = formato;
        this.TamanhoBytes = tamanhoBytes;
        this.CarregadoPorId = carregadoPorId;
        this.CarregadoEmUtc = carregadoEmUtc;
    }

    private Documento()
    {
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
    /// Gets o identificador do tratamento, quando é a receita de um tratamento.
    /// </summary>
    public Guid? TratamentoId { get; private set; }

    /// <summary>
    /// Gets a categoria do documento.
    /// </summary>
    public CategoriaDocumento Categoria { get; private set; }

    /// <summary>
    /// Gets o nome original do ficheiro.
    /// </summary>
    public string NomeOriginal { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o caminho interno do ficheiro, com nome gerado pelo servidor.
    /// </summary>
    public string CaminhoRelativo { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o formato do ficheiro.
    /// </summary>
    public FormatoDocumento Formato { get; private set; }

    /// <summary>
    /// Gets o tamanho do ficheiro, em bytes.
    /// </summary>
    public int TamanhoBytes { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que carregou o documento.
    /// </summary>
    public Guid CarregadoPorId { get; private set; }

    /// <summary>
    /// Gets o instante do carregamento, em UTC.
    /// </summary>
    public DateTime CarregadoEmUtc { get; private set; }
}
