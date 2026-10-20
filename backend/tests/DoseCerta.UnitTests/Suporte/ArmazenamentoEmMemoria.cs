// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Armazenamento de documentos em memória, com falhas que os testes podem provocar (RF-023, RF-124).
/// </summary>
public sealed class ArmazenamentoEmMemoria : IArmazenamentoDocumentos
{
    /// <summary>
    /// Gets os ficheiros guardados, por caminho relativo.
    /// </summary>
    public Dictionary<string, byte[]> Ficheiros { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets os caminhos cuja remoção foi pedida.
    /// </summary>
    public List<string> Eliminados { get; } = new();

    /// <summary>
    /// Gets or sets o número de gravações que ainda são aceites antes de falhar; <see langword="null"/> nunca falha.
    /// </summary>
    public int? GravacoesAntesDeFalhar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a leitura falha.
    /// </summary>
    public bool FalharLeitura { get; set; }

    /// <inheritdoc/>
    public async Task<string> GuardarAsync(Stream conteudo, FormatoDocumento formato, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        if (this.GravacoesAntesDeFalhar is int restantes)
        {
            if (restantes <= 0)
            {
                throw new IOException("Disco indisponível (simulado).");
            }

            this.GravacoesAntesDeFalhar = restantes - 1;
        }

        using var copia = new MemoryStream();
        await conteudo.CopyToAsync(copia, cancelamento);
        var caminho = $"{Guid.NewGuid():N}.{formato.ToString().ToLowerInvariant()}";
        this.Ficheiros[caminho] = copia.ToArray();
        return caminho;
    }

    /// <inheritdoc/>
    public Stream Abrir(string caminhoRelativo)
    {
        if (this.FalharLeitura || !this.Ficheiros.TryGetValue(caminhoRelativo, out var conteudo))
        {
            throw new FileNotFoundException("Ficheiro indisponível (simulado).", caminhoRelativo);
        }

        return new MemoryStream(conteudo, writable: false);
    }

    /// <inheritdoc/>
    public void Eliminar(string caminhoRelativo)
    {
        this.Eliminados.Add(caminhoRelativo);
        this.Ficheiros.Remove(caminhoRelativo);
    }
}
