// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Documentos;

/// <summary>
/// Documento que cumpre o RF-022 e o RNF-008, pronto a guardar.
/// </summary>
/// <param name="Categoria">Categoria do documento.</param>
/// <param name="Formato">Formato detetado pelo conteúdo do ficheiro.</param>
/// <param name="NomeOriginal">Nome original do ficheiro, sem caminho.</param>
/// <param name="TamanhoBytes">Tamanho do ficheiro, em bytes.</param>
/// <param name="AbrirLeitura">Abre o conteúdo do ficheiro para leitura.</param>
public sealed record DocumentoValidado(
    CategoriaDocumento Categoria,
    FormatoDocumento Formato,
    string NomeOriginal,
    int TamanhoBytes,
    Func<Stream> AbrirLeitura);
