// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Documento a associar a uma ficha na criação.
/// </summary>
/// <param name="Categoria">Categoria do documento.</param>
/// <param name="NomeFicheiro">Nome original do ficheiro.</param>
/// <param name="TipoConteudo">Tipo MIME declarado.</param>
/// <param name="TamanhoBytes">Tamanho em bytes.</param>
/// <param name="AbrirConteudo">Abre o conteúdo do ficheiro para leitura.</param>
public record NovoDocumento(
    CategoriaDocumento Categoria,
    string NomeFicheiro,
    string TipoConteudo,
    long TamanhoBytes,
    Func<Stream> AbrirConteudo);
