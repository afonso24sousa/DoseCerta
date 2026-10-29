// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Unidade de trabalho que executa a operação e falha depois, como se a gravação na base de dados fosse recusada; repõe as
/// ocorrências, as notas e as notificações como estavam antes, tal como a reversão da transação (RNF-046).
/// </summary>
public sealed class UnidadeDeTrabalhoQueFalhaAoGravar : IUnidadeDeTrabalho
{
    private readonly RepositoriosEmMemoria repositorios;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoQueFalhaAoGravar"/> class.
    /// </summary>
    /// <param name="repositorios">Repositórios a repor quando a gravação falha.</param>
    public UnidadeDeTrabalhoQueFalhaAoGravar(RepositoriosEmMemoria repositorios)
    {
        this.repositorios = repositorios;
    }

    /// <inheritdoc/>
    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        var ocorrencias = new List<Ocorrencia>(this.repositorios.Ocorrencias);
        var notas = new List<Nota>(this.repositorios.Notas);
        var notificacoes = new List<Notificacao>(this.repositorios.Notificacoes);

        await operacao();

        Repor(this.repositorios.Ocorrencias, ocorrencias);
        Repor(this.repositorios.Notas, notas);
        Repor(this.repositorios.Notificacoes, notificacoes);
        throw new InvalidOperationException("Falha simulada na gravação.");
    }

    /// <inheritdoc/>
    public void AoFalhar(Action compensacao)
    {
    }

    /// <inheritdoc/>
    public Task GravarAlteracoesAsync(CancellationToken cancelamento)
    {
        return Task.CompletedTask;
    }

    private static void Repor<T>(List<T> lista, List<T> anterior)
    {
        lista.Clear();
        lista.AddRange(anterior);
    }
}
