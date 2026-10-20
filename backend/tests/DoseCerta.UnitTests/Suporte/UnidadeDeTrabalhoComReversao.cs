// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Unidade de trabalho em memória que, quando a operação falha, repõe as fichas, os documentos e os convites
/// como estavam antes e executa as compensações, como a transação da base de dados (RNF-046).
/// </summary>
public sealed class UnidadeDeTrabalhoComReversao : IUnidadeDeTrabalho
{
    private readonly RepositoriosEmMemoria repositorios;
    private readonly List<Action> compensacoes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalhoComReversao"/> class.
    /// </summary>
    /// <param name="repositorios">Repositórios a repor quando a operação falha.</param>
    public UnidadeDeTrabalhoComReversao(RepositoriosEmMemoria repositorios)
    {
        this.repositorios = repositorios;
    }

    /// <inheritdoc/>
    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        this.compensacoes.Clear();
        var fichas = new List<Ficha>(this.repositorios.Fichas);
        var documentos = new List<Documento>(this.repositorios.Documentos);
        var convites = new List<Convite>(this.repositorios.Convites);
        try
        {
            return await operacao();
        }
        catch
        {
            Repor(this.repositorios.Fichas, fichas);
            Repor(this.repositorios.Documentos, documentos);
            Repor(this.repositorios.Convites, convites);
            this.compensacoes.ForEach(c => c());
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

    private static void Repor<T>(List<T> lista, List<T> anterior)
    {
        lista.Clear();
        lista.AddRange(anterior);
    }
}
