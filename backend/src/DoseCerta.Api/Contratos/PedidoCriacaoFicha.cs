// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.ComponentModel.DataAnnotations;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Pedido de criação de uma ficha (<c>multipart/form-data</c>), partilhado pela F002 e pela F003.
/// </summary>
public class PedidoCriacaoFicha
{
    /// <summary>
    /// Gets or sets a value indicating whether a ficha é para o próprio Utilizador autenticado (F003).
    /// </summary>
    public bool ParaOUtilizadorAutenticado { get; set; }

    /// <summary>
    /// Gets or sets o nome completo do utente.
    /// </summary>
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(200, ErrorMessage = "O nome completo não pode ter mais de 200 caracteres.")]
    public string? NomeCompleto { get; set; }

    /// <summary>
    /// Gets or sets a data de nascimento (formato <c>aaaa-mm-dd</c>).
    /// </summary>
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    public DateOnly? DataNascimento { get; set; }

    /// <summary>
    /// Gets or sets o nome do contacto de emergência.
    /// </summary>
    [Required(ErrorMessage = "O nome do contacto de emergência é obrigatório.")]
    [StringLength(200, ErrorMessage = "O nome do contacto de emergência não pode ter mais de 200 caracteres.")]
    public string? ContactoEmergenciaNome { get; set; }

    /// <summary>
    /// Gets or sets o telefone do contacto de emergência.
    /// </summary>
    [Required(ErrorMessage = "O telefone do contacto de emergência é obrigatório.")]
    [RegularExpression(@"^\+?[0-9 ]{9,20}$", ErrorMessage = "O telefone do contacto de emergência não é válido.")]
    public string? ContactoEmergenciaTelefone { get; set; }

    /// <summary>
    /// Gets or sets o grau de autonomia e mobilidade (categoria fechada).
    /// </summary>
    [Required(ErrorMessage = "O grau de autonomia é obrigatório.")]
    public int? GrauAutonomiaId { get; set; }

    /// <summary>
    /// Gets as condições de saúde (categoria fechada); usar a opção de ausência quando não existirem.
    /// </summary>
    public IList<int> CondicoesSaudeIds { get; } = new List<int>();

    /// <summary>
    /// Gets as alergias (categoria fechada); usar a opção de ausência quando não existirem.
    /// </summary>
    public IList<int> AlergiasIds { get; } = new List<int>();

    /// <summary>
    /// Gets os documentos a associar.
    /// </summary>
    public IList<PedidoDocumento> Documentos { get; } = new List<PedidoDocumento>();
}
