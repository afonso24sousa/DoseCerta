// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Documento de uma ficha.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Categoria">Categoria.</param>
/// <param name="NomeOriginal">Nome original do ficheiro.</param>
/// <param name="TipoConteudo">Tipo MIME.</param>
/// <param name="TamanhoBytes">Tamanho em bytes.</param>
/// <param name="DataCarregamento">Data do carregamento (UTC).</param>
public record RespostaDocumento(
    Guid Id,
    CategoriaDocumento Categoria,
    string NomeOriginal,
    string TipoConteudo,
    long TamanhoBytes,
    DateTime DataCarregamento);
