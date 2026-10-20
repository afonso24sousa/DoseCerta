// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Fichas;

/// <summary>
/// Metadados de um documento da ficha (OP-15 e OP-16); o conteúdo obtém-se em OP-17.
/// </summary>
/// <param name="Id">Identificador do documento.</param>
/// <param name="Categoria">Categoria do documento.</param>
/// <param name="NomeOriginal">Nome do ficheiro carregado.</param>
/// <param name="Formato">Formato do ficheiro: <c>PDF</c>, <c>JPG</c> ou <c>PNG</c>.</param>
/// <param name="TamanhoBytes">Tamanho do ficheiro, em bytes.</param>
/// <param name="CarregadoEm">Instante do carregamento, com o desvio de Lisboa.</param>
public sealed record RespostaDocumento(
    Guid Id,
    CategoriaDocumento Categoria,
    string NomeOriginal,
    FormatoDocumento Formato,
    int TamanhoBytes,
    DateTimeOffset CarregadoEm);
