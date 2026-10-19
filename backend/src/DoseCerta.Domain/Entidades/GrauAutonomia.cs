// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Valor da lista fechada de graus de autonomia ou mobilidade (RF-021).
/// </summary>
public class GrauAutonomia
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GrauAutonomia"/> class.
    /// </summary>
    /// <param name="id">Identificador do valor.</param>
    /// <param name="designacao">Designação apresentada.</param>
    /// <param name="ordem">Ordem de apresentação.</param>
    public GrauAutonomia(
        short id,
        string designacao,
        byte ordem)
    {
        this.Id = id;
        this.Designacao = designacao;
        this.Ordem = ordem;
    }

    private GrauAutonomia()
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

    /// <summary>
    /// Gets a ordem de apresentação, do mais autónomo para o mais dependente.
    /// </summary>
    public byte Ordem { get; private set; }
}
