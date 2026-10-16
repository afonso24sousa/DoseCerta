// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Estado do envio de um e-mail (RF-118, RF-119, RF-136).
/// </summary>
public enum EstadoEnvioEmail
{
    /// <summary>
    /// Por enviar ou a aguardar reenvio.
    /// </summary>
    Pendente,

    /// <summary>
    /// Aceite pelo serviço de e-mail.
    /// </summary>
    Enviado,

    /// <summary>
    /// Três reenvios falhados.
    /// </summary>
    NaoEntregue,
}
