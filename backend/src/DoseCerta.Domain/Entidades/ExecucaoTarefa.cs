// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Execução prevista de uma tarefa recorrente num dia e hora; copia a designação e a tolerância (RF-073, DM-17). O comportamento é implementado com a F007.
/// </summary>
public class ExecucaoTarefa
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecucaoTarefa"/> class.
    /// </summary>
    /// <param name="tarefaId">Identificador da tarefa.</param>
    /// <param name="previstaEmUtc">Instante previsto, em UTC.</param>
    /// <param name="designacao">Designação.</param>
    /// <param name="toleranciaMinutos">Tolerância em minutos.</param>
    public ExecucaoTarefa(
        Guid tarefaId,
        DateTime previstaEmUtc,
        string designacao,
        short toleranciaMinutos)
    {
        this.Id = Guid.NewGuid();
        this.TarefaId = tarefaId;
        this.PrevistaEmUtc = previstaEmUtc;
        this.Designacao = designacao;
        this.ToleranciaMinutos = toleranciaMinutos;
        this.Estado = EstadoExecucaoTarefa.PorConfirmar;
    }

    private ExecucaoTarefa()
    {
    }

    /// <summary>
    /// Gets o identificador da execução.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o identificador da tarefa.
    /// </summary>
    public Guid TarefaId { get; private set; }

    /// <summary>
    /// Gets o instante previsto, em UTC.
    /// </summary>
    public DateTime PrevistaEmUtc { get; private set; }

    /// <summary>
    /// Gets a designação copiada da tarefa.
    /// </summary>
    public string Designacao { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a tolerância copiada da tarefa, em minutos.
    /// </summary>
    public short ToleranciaMinutos { get; private set; }

    /// <summary>
    /// Gets o estado da execução.
    /// </summary>
    public EstadoExecucaoTarefa Estado { get; private set; }

    /// <summary>
    /// Gets o identificador do Cuidador que confirmou.
    /// </summary>
    public Guid? ConfirmadaPorId { get; private set; }

    /// <summary>
    /// Gets o instante da confirmação, em UTC.
    /// </summary>
    public DateTime? ConfirmadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante da notificação aos Cuidadores, em UTC.
    /// </summary>
    public DateTime? CuidadoresNotificadosEmUtc { get; private set; }

    /// <summary>
    /// Gets o token de concorrência otimista (AD-11).
    /// </summary>
    public byte[] Versao { get; private set; } = Array.Empty<byte>();
}
