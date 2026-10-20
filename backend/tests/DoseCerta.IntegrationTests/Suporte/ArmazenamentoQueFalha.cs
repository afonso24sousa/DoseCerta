// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Armazenamento de documentos que nunca consegue guardar, para provocar a falha do RF-023 com a API completa.
/// </summary>
public sealed class ArmazenamentoQueFalha : IArmazenamentoDocumentos
{
    /// <inheritdoc/>
    public Task<string> GuardarAsync(Stream conteudo, FormatoDocumento formato, CancellationToken cancelamento)
    {
        throw new IOException("Disco indisponível (simulado).");
    }

    /// <inheritdoc/>
    public Stream Abrir(string caminhoRelativo)
    {
        throw new IOException("Disco indisponível (simulado).");
    }

    /// <inheritdoc/>
    public void Eliminar(string caminhoRelativo)
    {
    }
}
