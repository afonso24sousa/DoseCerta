// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;
using DoseCerta.Infrastructure.Documentos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DoseCerta.UnitTests.Infraestrutura;

/// <summary>
/// Testes unitários do armazenamento de documentos no sistema de ficheiros, numa pasta temporária (RNF-006, RNF-007).
/// </summary>
public sealed class ArmazenamentoSistemaFicheirosTestes : IDisposable
{
    private readonly string pasta = Path.Combine(Path.GetTempPath(), $"dosecerta-testes-{Guid.NewGuid():N}");
    private readonly ArmazenamentoSistemaFicheiros armazenamento;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArmazenamentoSistemaFicheirosTestes"/> class.
    /// </summary>
    public ArmazenamentoSistemaFicheirosTestes()
    {
        this.armazenamento = new ArmazenamentoSistemaFicheiros(
            Options.Create(new OpcoesDocumentos { PastaRaiz = this.pasta }),
            NullLogger<ArmazenamentoSistemaFicheiros>.Instance);
    }

    /// <summary>
    /// O ficheiro é guardado com um nome gerado pelo servidor e lido de volta com o mesmo conteúdo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GuardarEAbrirDevolvemOMesmoConteudo()
    {
        var conteudo = new byte[] { 1, 2, 3, 4 };

        var caminho = await this.armazenamento.GuardarAsync(new MemoryStream(conteudo), FormatoDocumento.PDF, CancellationToken.None);

        Assert.EndsWith(".pdf", caminho, StringComparison.Ordinal);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(caminho), out _));
        await using var lido = this.armazenamento.Abrir(caminho);
        using var copia = new MemoryStream();
        await lido.CopyToAsync(copia);
        Assert.Equal(conteudo, copia.ToArray());
    }

    /// <summary>
    /// A remoção apaga o ficheiro; remover um ficheiro inexistente não falha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarApagaOFicheiro()
    {
        var caminho = await this.armazenamento.GuardarAsync(new MemoryStream(new byte[] { 1 }), FormatoDocumento.PNG, CancellationToken.None);

        this.armazenamento.Eliminar(caminho);
        this.armazenamento.Eliminar(caminho);

        Assert.False(File.Exists(Path.Combine(this.pasta, caminho)));
        Assert.Throws<FileNotFoundException>(() => this.armazenamento.Abrir(caminho));
    }

    /// <summary>
    /// RNF-007: um caminho fora da pasta dos documentos nunca é aberto.
    /// </summary>
    [Fact]
    public void CaminhoForaDaPastaERecusado()
    {
        Assert.Throws<IOException>(() => this.armazenamento.Abrir(Path.Combine("..", "fora.pdf")));
    }

    /// <summary>
    /// Sem pasta configurada, usa a pasta de dados locais do utilizador.
    /// </summary>
    [Fact]
    public void PastaPorOmissaoFicaNosDadosLocais()
    {
        var pastaPorOmissao = new OpcoesDocumentos().PastaEfetiva();

        Assert.True(Path.IsPathFullyQualified(pastaPorOmissao));
        Assert.EndsWith(Path.Combine("DoseCerta", "documentos"), pastaPorOmissao, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(this.pasta))
        {
            Directory.Delete(this.pasta, recursive: true);
        }
    }
}
