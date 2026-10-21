// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Pessoas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às fichas (FichaRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface IFichaRepositorio
{
    /// <summary>
    /// Obtém uma ficha com as associações e as necessidades específicas.
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>A ficha, ou <see langword="null"/> se não existir.</returns>
    Task<Ficha?> ObterCompletaAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Lista as fichas a que um Utilizador está associado, com as associações (RNF-005).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="estado">Estado pedido, ou <see langword="null"/> para ativas e arquivadas.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As fichas, ordenadas pelo nome do utente.</returns>
    Task<IReadOnlyList<Ficha>> ListarDoUtilizadorAsync(Guid utilizadorId, EstadoFicha? estado, CancellationToken cancelamento);

    /// <summary>
    /// Lista as pessoas associadas a uma ficha, com o nome e o e-mail da conta: o Cuidador principal primeiro e depois por nome (OP-18).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As pessoas associadas.</returns>
    Task<IReadOnlyList<PessoaAssociada>> ListarPessoasAsync(Guid fichaId, CancellationToken cancelamento);

    /// <summary>
    /// Indica se o Utilizador já tem uma ficha própria (RF-031).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se já existir uma ficha própria.</returns>
    Task<bool> ExistePropriaDeAsync(Guid utilizadorId, CancellationToken cancelamento);

    /// <summary>
    /// Indica se a ficha tem medicação registada, incluindo tratamentos terminados (RF-123).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se existir pelo menos um tratamento.</returns>
    Task<bool> TemMedicacaoAsync(Guid fichaId, CancellationToken cancelamento);

    /// <summary>
    /// Indica se a ficha tem tarefas recorrentes registadas, incluindo as eliminadas (inativas) (RF-123).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se existir pelo menos uma tarefa.</returns>
    Task<bool> TemTarefasAsync(Guid fichaId, CancellationToken cancelamento);

    /// <summary>
    /// Indica se a ficha tem histórico: tomas, execuções de tarefas, ocorrências ou notas (RF-123).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se existir histórico.</returns>
    Task<bool> TemHistoricoAsync(Guid fichaId, CancellationToken cancelamento);

    /// <summary>
    /// Apaga as tomas e as execuções de tarefas futuras ainda por confirmar da ficha, ao arquivar (RF-122).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="aPartirDeUtc">Instante do arquivo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de linhas apagadas.</returns>
    Task<int> EliminarFuturasPorConfirmarAsync(Guid fichaId, DateTime aPartirDeUtc, CancellationToken cancelamento);

    /// <summary>
    /// Regista uma ficha nova.
    /// </summary>
    /// <param name="ficha">Ficha a registar.</param>
    void Adicionar(Ficha ficha);

    /// <summary>
    /// Remove uma ficha; as associações, os convites, os compromissos e os documentos saem com ela (RF-123).
    /// </summary>
    /// <param name="ficha">Ficha a remover.</param>
    void Remover(Ficha ficha);
}
