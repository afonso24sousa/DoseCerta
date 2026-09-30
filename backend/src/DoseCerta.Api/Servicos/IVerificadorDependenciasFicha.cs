// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Indica se uma ficha tem medicação, tarefas ou histórico, caso em que só pode ser arquivada.
/// </summary>
public interface IVerificadorDependenciasFicha
{
    /// <summary>
    /// Indica se a ficha tem medicação, tarefas ou histórico.
    /// </summary>
    /// <param name="fichaId">Ficha a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se a ficha não puder ser eliminada.</returns>
    Task<bool> TemDependenciasAsync(Guid fichaId, CancellationToken cancellationToken);
}
