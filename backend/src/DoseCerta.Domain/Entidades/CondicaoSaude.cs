// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Valor da lista fechada de condições de saúde (RF-021).
/// </summary>
public class CondicaoSaude
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CondicaoSaude"/> class.
    /// </summary>
    /// <param name="id">Identificador do valor.</param>
    /// <param name="designacao">Designação apresentada.</param>
    public CondicaoSaude(
        short id,
        string designacao)
    {
        this.Id = id;
        this.Designacao = designacao;
    }

    private CondicaoSaude()
    {
    }

    /// <summary>
    /// Gets o identificador do valor.
    /// </summary>
    public short Id { get; private set; }

    /// <summary>
    /// Gets a designação apresentada.
    /// </summary>
    public string Designacao { get; private set; } = string.Empty;
}
