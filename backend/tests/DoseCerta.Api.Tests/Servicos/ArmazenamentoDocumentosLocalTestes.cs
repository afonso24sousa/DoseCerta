// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text;
using DoseCerta.Api.Servicos;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace DoseCerta.Api.Tests.Servicos;

/// <summary>
/// Testes do armazenamento local de documentos.
/// </summary>
public sealed class ArmazenamentoDocumentosLocalTestes : IDisposable
{
    private readonly string raiz = Path.Combine(Path.GetTempPath(), "dosecerta-testes-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Guardar escreve o ficheiro completo e devolve uma chave com a extensão original; remover apaga-o.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task GuardarERemoverFicheiro()
    {
        var armazenamento = this.CriarArmazenamento();
        var conteudo = Encoding.ASCII.GetBytes("%PDF-1.4 documento ficticio");

        using var origem = new MemoryStream(conteudo);
        var chave = await armazenamento.GuardarAsync(origem, "Receita.PDF", CancellationToken.None);

        var caminho = Path.Combine(this.raiz, "documentos", chave);
        Assert.EndsWith(".pdf", chave, StringComparison.Ordinal);
        Assert.Equal(conteudo, await File.ReadAllBytesAsync(caminho));

        await armazenamento.RemoverAsync(chave, CancellationToken.None);
        Assert.False(File.Exists(caminho));
    }

    /// <summary>
    /// Uma falha a meio da escrita não deixa ficheiros parciais.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNaEscritaNaoDeixaFicheiroParcial()
    {
        var armazenamento = this.CriarArmazenamento();
        var origem = new Mock<Stream>();
        origem.Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Falha simulada de leitura."));

        await Assert.ThrowsAsync<IOException>(() => armazenamento.GuardarAsync(origem.Object, "relatorio.pdf", CancellationToken.None));

        Assert.Empty(Directory.GetFiles(Path.Combine(this.raiz, "documentos")));
    }

    /// <summary>
    /// A remoção ignora qualquer caminho na chave e nunca sai da pasta configurada.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task RemoverNaoSaiDaPastaConfigurada()
    {
        var armazenamento = this.CriarArmazenamento();
        Directory.CreateDirectory(this.raiz);
        var externo = Path.Combine(this.raiz, "externo.txt");
        await File.WriteAllTextAsync(externo, "não apagar");

        await armazenamento.RemoverAsync("../externo.txt", CancellationToken.None);

        Assert.True(File.Exists(externo));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(this.raiz))
        {
            Directory.Delete(this.raiz, recursive: true);
        }
    }

    private ArmazenamentoDocumentosLocal CriarArmazenamento()
    {
        var ambiente = new Mock<IHostEnvironment>();
        ambiente.SetupGet(a => a.ContentRootPath).Returns(this.raiz);
        ambiente.SetupGet(a => a.ContentRootFileProvider).Returns(new NullFileProvider());
        var opcoes = Options.Create(new OpcoesArmazenamento { DiretorioDocumentos = "documentos" });
        return new ArmazenamentoDocumentosLocal(opcoes, ambiente.Object);
    }
}
