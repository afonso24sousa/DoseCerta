// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Enumerados;

/// <summary>
/// Estado de uma ficha (RF-120, RF-121).
/// </summary>
public enum EstadoFicha
{
    /// <summary>
    /// Ficha em utilização.
    /// </summary>
    Ativa,

    /// <summary>
    /// Ficha só de consulta, sem geração de tomas, avisos nem notificações (RF-122).
    /// </summary>
    Arquivada,
}
