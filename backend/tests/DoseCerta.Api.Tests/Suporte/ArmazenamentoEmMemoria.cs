// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Collections.Concurrent;
using DoseCerta.Api.Servicos;

namespace DoseCerta.Api.Tests.Suporte;

/// <summary>
/// Armazenamento de documentos em memória, com a possibilidade de simular falhas de gravação.
/// </summary>
public sealed class ArmazenamentoEmMemoria : IArmazenamentoDocumentos
{
    private readonly ConcurrentDictionary<string, byte[]> ficheiros = new();

    /// <summary>
    /// Gets or sets o número de gravações bem-sucedidas antes de a seguinte falhar; nulo para nunca falhar.
    /// </summary>
    public int? FalharAposGravacoes { get; set; }

    /// <summary>
    /// Gets as chaves dos ficheiros atualmente guardados.
    /// </summary>
    public ICollection<string> Chaves => this.ficheiros.Keys;

    /// <inheritdoc/>
    public async Task<string> GuardarAsync(Stream conteudo, string nomeFicheiro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conteudo);

        if (this.FalharAposGravacoes is { } limite)
        {
            if (limite == 0)
            {
                throw new IOException("Falha simulada de gravação.");
            }

            this.FalharAposGravacoes = limite - 1;
        }

        using var memoria = new MemoryStream();
        await conteudo.CopyToAsync(memoria, cancellationToken);
        var chave = Guid.NewGuid().ToString("N") + Path.GetExtension(nomeFicheiro);
        this.ficheiros[chave] = memoria.ToArray();
        return chave;
    }

    /// <inheritdoc/>
    public Task RemoverAsync(string chave, CancellationToken cancellationToken)
    {
        this.ficheiros.TryRemove(chave, out _);
        return Task.CompletedTask;
    }
}
