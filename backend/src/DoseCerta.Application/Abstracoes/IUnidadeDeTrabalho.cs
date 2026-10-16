// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Abstracoes;

/// <summary>
/// Executa cada operação de escrita numa única transação e executa as compensações registadas
/// se a transação falhar, por exemplo apagar um ficheiro já guardado (RNF-046, AD-10).
/// </summary>
public interface IUnidadeDeTrabalho
{
    /// <summary>
    /// Executa a operação numa transação, guarda as alterações e confirma a transação.
    /// Se a operação ou a gravação falhar, desfaz a transação, executa as compensações e volta a lançar o erro.
    /// </summary>
    /// <typeparam name="T">Tipo do resultado da operação.</typeparam>
    /// <param name="operacao">Operação a executar.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O resultado da operação.</returns>
    Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken cancelamento);

    /// <summary>
    /// Regista uma compensação a executar se a transação em curso falhar.
    /// </summary>
    /// <param name="compensacao">Ação de compensação.</param>
    void AoFalhar(Action compensacao);

    /// <summary>
    /// Grava na base de dados, dentro da transação em curso, as alterações já feitas, sem confirmar a transação.
    /// Usa-se quando a ordem das escritas importa, por exemplo para respeitar uma restrição de unicidade (RNF-046).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    Task GravarAlteracoesAsync(CancellationToken cancelamento);
}
