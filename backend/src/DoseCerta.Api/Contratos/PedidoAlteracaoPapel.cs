// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.ComponentModel.DataAnnotations;
using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Pedido de alteração do papel de um Utilizador numa ficha.
/// </summary>
public class PedidoAlteracaoPapel
{
    /// <summary>
    /// Gets or sets o novo papel.
    /// </summary>
    [Required(ErrorMessage = "O papel é obrigatório.")]
    public PapelFicha? Papel { get; set; }
}
