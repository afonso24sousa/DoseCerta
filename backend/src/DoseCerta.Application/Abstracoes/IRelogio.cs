// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Tempo;

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Fornece o instante atual em UTC e o calendário de Lisboa; os testes fixam o relógio (RNF-047).
/// </summary>
public interface IRelogio
{
    /// <summary>
    /// Obtém o instante atual em UTC.
    /// </summary>
    /// <returns>O instante atual em UTC.</returns>
    DateTime AgoraUtc();

    /// <summary>
    /// Obtém o calendário de Lisboa usado nos cálculos de tempo.
    /// </summary>
    /// <returns>O calendário de Lisboa.</returns>
    CalendarioLisboa Calendario();
}
