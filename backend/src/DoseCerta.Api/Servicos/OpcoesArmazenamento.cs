// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Configuração do armazenamento local de documentos (secção <c>Armazenamento</c>).
/// </summary>
public class OpcoesArmazenamento
{
    /// <summary>
    /// Nome da secção de configuração.
    /// </summary>
    public const string Seccao = "Armazenamento";

    /// <summary>
    /// Gets or sets a pasta onde são guardados os ficheiros; um caminho relativo é resolvido a partir da raiz da aplicação.
    /// </summary>
    public string DiretorioDocumentos { get; set; } = "ficheiros/documentos";
}
