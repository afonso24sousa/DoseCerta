// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Tipo de notificação (RF-110 a RF-113, RF-115 a RF-117, RF-041).
/// </summary>
public enum TipoNotificacao
{
    /// <summary>
    /// Toma pendente depois da tolerância.
    /// </summary>
    TomaEmAtraso,

    /// <summary>
    /// Toma grave sem confirmação 60 minutos depois.
    /// </summary>
    TomaGraveFamiliares,

    /// <summary>
    /// Nova ocorrência registada.
    /// </summary>
    NovaOcorrencia,

    /// <summary>
    /// Aviso antecipado de um compromisso.
    /// </summary>
    ProximoCompromisso,

    /// <summary>
    /// Convite recebido para uma ficha.
    /// </summary>
    ConviteRecebido,
}
