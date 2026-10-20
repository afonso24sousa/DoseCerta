// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Infrastructure.Documentos;

/// <summary>
/// Configuração do armazenamento de documentos (secção <c>Documentos</c> da configuração, RNF-006).
/// </summary>
public sealed class OpcoesDocumentos
{
    /// <summary>
    /// Nome da secção de configuração.
    /// </summary>
    public const string Seccao = "Documentos";

    /// <summary>
    /// Gets or sets a pasta onde os ficheiros são guardados. Fica fora do conteúdo servido pela API, sem endereço público (RNF-007).
    /// Vazia: usa <c>DoseCerta/documentos</c> na pasta de dados locais do utilizador do sistema operativo.
    /// </summary>
    public string PastaRaiz { get; set; } = string.Empty;

    /// <summary>
    /// Obtém a pasta raiz efetiva, em caminho absoluto.
    /// </summary>
    /// <returns>O caminho absoluto da pasta dos documentos.</returns>
    public string PastaEfetiva()
    {
        var pasta = string.IsNullOrWhiteSpace(this.PastaRaiz)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoseCerta", "documentos")
            : this.PastaRaiz;
        return Path.GetFullPath(pasta);
    }
}
