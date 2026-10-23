// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos tratamentos (TratamentoRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface ITratamentoRepositorio
{
    /// <summary>
    /// Obtém um tratamento com os horários, para consulta ou alteração.
    /// </summary>
    /// <param name="id">Identificador do tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O tratamento, ou <see langword="null"/> se não existir.</returns>
    Task<Tratamento?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Lista os tratamentos de uma ficha, com os horários (RF-054).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="estado">Estado pedido, ou <see langword="null"/> para em curso e terminados.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os tratamentos, do início mais recente para o mais antigo.</returns>
    Task<IReadOnlyList<Tratamento>> DaFichaAsync(Guid fichaId, EstadoTratamento? estado, CancellationToken cancelamento);

    /// <summary>
    /// Regista um tratamento novo, com os horários.
    /// </summary>
    /// <param name="tratamento">Tratamento a registar.</param>
    void Adicionar(Tratamento tratamento);
}
