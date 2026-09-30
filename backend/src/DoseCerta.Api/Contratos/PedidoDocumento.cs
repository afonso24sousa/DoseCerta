// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.ComponentModel.DataAnnotations;
using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Documento enviado no pedido de criação de uma ficha.
/// </summary>
public class PedidoDocumento
{
    /// <summary>
    /// Gets or sets a categoria do documento.
    /// </summary>
    [Required(ErrorMessage = "A categoria do documento é obrigatória.")]
    public CategoriaDocumento? Categoria { get; set; }

    /// <summary>
    /// Gets or sets o ficheiro.
    /// </summary>
    [Required(ErrorMessage = "O ficheiro do documento é obrigatório.")]
    public IFormFile? Ficheiro { get; set; }
}
