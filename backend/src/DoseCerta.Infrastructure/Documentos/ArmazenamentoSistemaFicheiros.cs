// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Enumerados;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoseCerta.Infrastructure.Documentos;

/// <summary>
/// Implementação de <see cref="IArmazenamentoDocumentos"/> sobre uma pasta do servidor, sem serviços externos e sem endereço público
/// (RNF-006, RNF-007). O nome do ficheiro é gerado pelo servidor (GUID) e o conteúdo é escrito em fluxo (RNF-014).
/// </summary>
public sealed partial class ArmazenamentoSistemaFicheiros : IArmazenamentoDocumentos
{
    /// <summary>
    /// Número de tentativas de remoção de um ficheiro antes de registar a falha.
    /// </summary>
    public const int TentativasEliminacao = 3;

    private readonly string pastaRaiz;
    private readonly ILogger<ArmazenamentoSistemaFicheiros> registo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArmazenamentoSistemaFicheiros"/> class.
    /// </summary>
    /// <param name="opcoes">Configuração do armazenamento.</param>
    /// <param name="registo">Registo de eventos.</param>
    public ArmazenamentoSistemaFicheiros(IOptions<OpcoesDocumentos> opcoes, ILogger<ArmazenamentoSistemaFicheiros> registo)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        this.pastaRaiz = opcoes.Value.PastaEfetiva();
        this.registo = registo;
    }

    /// <inheritdoc/>
    public async Task<string> GuardarAsync(Stream conteudo, FormatoDocumento formato, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        Directory.CreateDirectory(this.pastaRaiz);
        var caminhoRelativo = $"{Guid.NewGuid():N}.{formato.ToString().ToLowerInvariant()}";
        var caminho = Path.Combine(this.pastaRaiz, caminhoRelativo);
        try
        {
            var destino = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await using (destino.ConfigureAwait(false))
            {
                await conteudo.CopyToAsync(destino, cancelamento).ConfigureAwait(false);
            }
        }
        catch
        {
            // Um ficheiro escrito em parte nunca fica no armazenamento.
            File.Delete(caminho);
            throw;
        }

        return caminhoRelativo;
    }

    /// <inheritdoc/>
    public Stream Abrir(string caminhoRelativo)
    {
        return new FileStream(this.CaminhoAbsoluto(caminhoRelativo), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
    }

    /// <inheritdoc/>
    public void Eliminar(string caminhoRelativo)
    {
        for (var tentativa = 1; tentativa <= TentativasEliminacao; tentativa++)
        {
            try
            {
                File.Delete(this.CaminhoAbsoluto(caminhoRelativo));
                return;
            }
            catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
            {
                if (tentativa == TentativasEliminacao)
                {
                    this.RegistarFalhaEliminacao(caminhoRelativo, erro);
                }
            }
        }
    }

    private string CaminhoAbsoluto(string caminhoRelativo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caminhoRelativo);

        // O caminho vem sempre da base de dados, mas nunca pode sair da pasta dos documentos.
        var caminho = Path.GetFullPath(Path.Combine(this.pastaRaiz, caminhoRelativo));
        var raiz = Path.TrimEndingDirectorySeparator(this.pastaRaiz) + Path.DirectorySeparatorChar;
        if (!caminho.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("O caminho do documento está fora da pasta dos documentos.");
        }

        return caminho;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "Não foi possível remover o ficheiro do documento {Caminho}; fica por remover manualmente.")]
    private partial void RegistarFalhaEliminacao(string caminho, Exception erro);
}
