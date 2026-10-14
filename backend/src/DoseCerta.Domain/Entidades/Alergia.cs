// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Valor da lista fechada de alergias (RF-021).
/// </summary>
public class Alergia
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Alergia"/> class.
    /// </summary>
    /// <param name="id">Identificador do valor.</param>
    /// <param name="designacao">Designação apresentada.</param>
    public Alergia(
        short id,
        string designacao)
    {
        this.Id = id;
        this.Designacao = designacao;
    }

    private Alergia()
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
