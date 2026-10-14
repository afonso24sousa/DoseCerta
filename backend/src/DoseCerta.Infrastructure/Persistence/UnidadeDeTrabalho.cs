// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DoseCerta.Infrastructure.Persistence;

/// <summary>
/// Implementação de <see cref="IUnidadeDeTrabalho"/> sobre a transação do <see cref="DoseCertaDbContext"/>.
/// Uma falha (incluindo de concorrência ou de unicidade) desfaz a transação inteira, executa as compensações registadas
/// e volta a lançar o erro original.
/// </summary>
public sealed partial class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly DoseCertaDbContext contexto;
    private readonly ILogger<UnidadeDeTrabalho> registo;
    private readonly List<Action> compensacoes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnidadeDeTrabalho"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    /// <param name="registo">Registo de eventos.</param>
    public UnidadeDeTrabalho(DoseCertaDbContext contexto, ILogger<UnidadeDeTrabalho> registo)
    {
        this.contexto = contexto;
        this.registo = registo;
    }

    /// <inheritdoc/>
    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        this.compensacoes.Clear();

        await using var transacao = await this.contexto.Database.BeginTransactionAsync(cancelamento).ConfigureAwait(false);
        try
        {
            var resultado = await operacao().ConfigureAwait(false);
            await this.contexto.SaveChangesAsync(cancelamento).ConfigureAwait(false);
            await transacao.CommitAsync(cancelamento).ConfigureAwait(false);
            this.compensacoes.Clear();
            return resultado;
        }
        catch
        {
            await transacao.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            this.contexto.ChangeTracker.Clear();
            this.ExecutarCompensacoes();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task GravarAlteracoesAsync(CancellationToken cancelamento)
    {
        await this.contexto.SaveChangesAsync(cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void AoFalhar(Action compensacao)
    {
        ArgumentNullException.ThrowIfNull(compensacao);
        this.compensacoes.Add(compensacao);
    }

    private void ExecutarCompensacoes()
    {
        foreach (var compensacao in this.compensacoes)
        {
            try
            {
                compensacao();
            }
            catch (IOException erro)
            {
                this.RegistarFalhaCompensacao(erro);
            }
            catch (UnauthorizedAccessException erro)
            {
                this.RegistarFalhaCompensacao(erro);
            }
        }

        this.compensacoes.Clear();
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Falha ao executar uma compensação depois de uma transação falhada.")]
    private partial void RegistarFalhaCompensacao(Exception erro);
}
