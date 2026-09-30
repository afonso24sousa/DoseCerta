// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Categorias fechadas de documentos que podem ser associados a uma ficha.
/// </summary>
public enum CategoriaDocumento
{
    /// <summary>
    /// Receita médica.
    /// </summary>
    ReceitaMedica = 1,

    /// <summary>
    /// Relatório clínico.
    /// </summary>
    RelatorioClinico = 2,

    /// <summary>
    /// Documento de identificação.
    /// </summary>
    DocumentoIdentificacao = 3,
}
