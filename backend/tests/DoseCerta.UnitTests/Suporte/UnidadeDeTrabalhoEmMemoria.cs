// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Unidade de trabalho para os testes unitários: executa a operação e, se falhar, executa as compensações.
/// Conta as operações confirmadas, para verificar que uma operação recusada não grava nada.
/// </summary>
public sealed class UnidadeDeTrabalhoEmMemoria : IUnidadeDeTrabalho
{
    private readonly List<Action> compensacoes = new();

    /// <summary>
    /// Gets o número de operações concluídas com sucesso.
    /// </summary>
    public int Confirmadas { get; private set; }

    /// <summary>
    /// Gets o número de compensações executadas.
    /// </summary>
    public int CompensacoesExecutadas { get; private set; }

    /// <summary>
    /// Gets or sets a ação executada antes de cada confirmação (para simular falhas de gravação).
    /// </summary>
    public Action? AntesDeConfirmar { get; set; }

    /// <inheritdoc/>
    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        this.compensacoes.Clear();
        try
        {
            var resultado = await operacao();
            this.AntesDeConfirmar?.Invoke();
            this.Confirmadas++;
            return resultado;
        }
        catch
        {
            foreach (var compensacao in this.compensacoes)
            {
                compensacao();
                this.CompensacoesExecutadas++;
            }

            throw;
        }
        finally
        {
            this.compensacoes.Clear();
        }
    }

    /// <inheritdoc/>
    public void AoFalhar(Action compensacao)
    {
        this.compensacoes.Add(compensacao);
    }

    /// <inheritdoc/>
    public Task GravarAlteracoesAsync(CancellationToken cancelamento)
    {
        return Task.CompletedTask;
    }
}
