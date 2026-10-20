// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text;
using DoseCerta.Application.Documentos;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Conteúdos fictícios de ficheiros para os testes de documentos (RNF-008).
/// </summary>
public static class FicheirosTeste
{
    /// <summary>
    /// Gets o conteúdo de um PDF mínimo.
    /// </summary>
    public static byte[] Pdf { get; } = Encoding.ASCII.GetBytes("%PDF-1.4\n%fictício\n%%EOF");

    /// <summary>
    /// Gets o cabeçalho de um PNG.
    /// </summary>
    public static byte[] Png { get; } = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };

    /// <summary>
    /// Gets o cabeçalho de um JPG.
    /// </summary>
    public static byte[] Jpg { get; } = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

    /// <summary>
    /// Gets um conteúdo de texto, num formato não suportado.
    /// </summary>
    public static byte[] Texto { get; } = Encoding.UTF8.GetBytes("apenas texto");

    /// <summary>
    /// Cria um documento submetido com o conteúdo indicado.
    /// </summary>
    /// <param name="conteudo">Conteúdo do ficheiro.</param>
    /// <param name="categoria">Categoria indicada.</param>
    /// <param name="nome">Nome do ficheiro.</param>
    /// <param name="tamanho">Tamanho declarado; por omissão, o do conteúdo.</param>
    /// <returns>O documento submetido.</returns>
    public static DocumentoSubmetido Documento(byte[] conteudo, string? categoria = "RelatorioClinico", string? nome = "relatorio.pdf", long? tamanho = null)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        return new DocumentoSubmetido(categoria, nome, tamanho ?? conteudo.Length, () => new MemoryStream(conteudo, writable: false));
    }
}
