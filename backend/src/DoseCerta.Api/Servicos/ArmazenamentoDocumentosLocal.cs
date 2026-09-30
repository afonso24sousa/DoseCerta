// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using Microsoft.Extensions.Options;

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Armazenamento de documentos no sistema de ficheiros local.
/// </summary>
public sealed class ArmazenamentoDocumentosLocal : IArmazenamentoDocumentos
{
    private readonly string diretorio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArmazenamentoDocumentosLocal"/> class.
    /// </summary>
    /// <param name="opcoes">Configuração do armazenamento.</param>
    /// <param name="ambiente">Ambiente de execução, usado para resolver caminhos relativos.</param>
    public ArmazenamentoDocumentosLocal(IOptions<OpcoesArmazenamento> opcoes, IHostEnvironment ambiente)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        ArgumentNullException.ThrowIfNull(ambiente);

        this.diretorio = Path.GetFullPath(opcoes.Value.DiretorioDocumentos, ambiente.ContentRootPath);
    }

    /// <inheritdoc/>
    public async Task<string> GuardarAsync(Stream conteudo, string nomeFicheiro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        Directory.CreateDirectory(this.diretorio);
        var chave = Guid.NewGuid().ToString("N") + Path.GetExtension(nomeFicheiro).ToLowerInvariant();
        var caminho = Path.Combine(this.diretorio, chave);

        try
        {
            await using (var destino = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await conteudo.CopyToAsync(destino, cancellationToken);
                await destino.FlushAsync(cancellationToken);
            }
        }
        catch
        {
            File.Delete(caminho);
            throw;
        }

        return chave;
    }

    /// <inheritdoc/>
    public Task RemoverAsync(string chave, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);

        // A chave é gerada por este serviço; Path.GetFileName impede sair da pasta configurada.
        var caminho = Path.Combine(this.diretorio, Path.GetFileName(chave));
        if (File.Exists(caminho))
        {
            File.Delete(caminho);
        }

        return Task.CompletedTask;
    }
}
