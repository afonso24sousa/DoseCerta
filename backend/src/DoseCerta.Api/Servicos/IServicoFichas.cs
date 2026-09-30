// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Gestão das fichas e das regras de negócio associadas.
/// </summary>
public interface IServicoFichas
{
    /// <summary>
    /// Cria uma ficha. Com <see cref="NovaFicha.ParaOUtilizadorAutenticado"/> ativo cria a ficha própria (F003).
    /// A criação é transacional: se algum documento não for guardado, nada fica gravado.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="pedido">Dados da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha criada, com as relações carregadas.</returns>
    Task<Ficha> CriarAsync(Guid utilizadorId, NovaFicha pedido, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma ficha à qual o Utilizador está associado.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pretendida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha, com as relações carregadas.</returns>
    Task<Ficha> ObterAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém a ficha própria do Utilizador, se existir.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha própria ou <see langword="null"/>.</returns>
    Task<Ficha?> ObterPropriaAsync(Guid utilizadorId, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina uma ficha sem medicação, tarefas nem histórico.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task EliminarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken);

    /// <summary>
    /// Arquiva uma ficha.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a arquivar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task ArquivarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken);

    /// <summary>
    /// Reativa uma ficha arquivada.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha a reativar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task ReativarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken);

    /// <summary>
    /// Altera o papel de um Utilizador associado à ficha (só um Cuidador o pode fazer).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="alvoId">Utilizador cujo papel é alterado.</param>
    /// <param name="novoPapel">Novo papel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task AlterarPapelAsync(Guid utilizadorId, Guid fichaId, Guid alvoId, PapelFicha novoPapel, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a associação de um Utilizador à ficha (só um Cuidador o pode fazer).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="alvoId">Utilizador a remover.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tarefa assíncrona.</returns>
    Task RemoverAssociacaoAsync(Guid utilizadorId, Guid fichaId, Guid alvoId, CancellationToken cancellationToken);
}
