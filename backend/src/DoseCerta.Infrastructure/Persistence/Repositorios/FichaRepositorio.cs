// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Pessoas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositório EF Core das fichas. As consultas partem sempre das associações do Utilizador (RNF-005).
/// </summary>
public sealed class FichaRepositorio : IFichaRepositorio
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichaRepositorio"/> class.
    /// </summary>
    /// <param name="contexto">Contexto da base de dados.</param>
    public FichaRepositorio(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <inheritdoc/>
    public Task<Ficha?> ObterCompletaAsync(Guid id, CancellationToken cancelamento)
    {
        return this.contexto.Fichas
            .Include(f => f.Associacoes)
            .Include(f => f.GrauAutonomia)
            .Include(f => f.CondicoesSaude)
            .Include(f => f.Alergias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == id, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Ficha>> ListarDoUtilizadorAsync(Guid utilizadorId, EstadoFicha? estado, CancellationToken cancelamento)
    {
        var consulta = this.contexto.Fichas
            .AsNoTracking()
            .Include(f => f.Associacoes.Where(a => a.UtilizadorId == utilizadorId))
            .Where(f => f.Associacoes.Any(a => a.UtilizadorId == utilizadorId));
        if (estado is not null)
        {
            consulta = consulta.Where(f => f.Estado == estado);
        }

        return await consulta
            .OrderBy(f => f.NomeCompleto)
            .ThenBy(f => f.Id)
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PessoaAssociada>> ListarPessoasAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return await this.contexto.Associacoes
            .AsNoTracking()
            .Where(a => a.FichaId == fichaId)
            .Join(this.contexto.Utilizadores, a => a.UtilizadorId, u => u.Id, (a, u) => new { Associacao = a, Utilizador = u })
            .OrderByDescending(p => p.Associacao.EPrincipal)
            .ThenBy(p => p.Utilizador.Nome)
            .ThenBy(p => p.Utilizador.Id)
            .Select(p => new PessoaAssociada(
                p.Utilizador.Id,
                p.Utilizador.Nome,
                p.Utilizador.Email,
                p.Associacao.Papel,
                p.Associacao.EPrincipal,
                p.Associacao.EProvisorio))
            .ToListAsync(cancelamento)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<bool> ExistePropriaDeAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        return this.contexto.Fichas.AnyAsync(f => f.UtilizadorProprioId == utilizadorId, cancelamento);
    }

    /// <inheritdoc/>
    public Task<bool> TemMedicacaoAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return this.contexto.Tratamentos.AnyAsync(t => t.FichaId == fichaId, cancelamento);
    }

    /// <inheritdoc/>
    public Task<bool> TemTarefasAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return this.contexto.Tarefas.AnyAsync(t => t.FichaId == fichaId, cancelamento);
    }

    /// <inheritdoc/>
    public async Task<bool> TemHistoricoAsync(Guid fichaId, CancellationToken cancelamento)
    {
        var tratamentos = this.contexto.Tratamentos.Where(t => t.FichaId == fichaId).Select(t => t.Id);
        var tarefas = this.contexto.Tarefas.Where(t => t.FichaId == fichaId).Select(t => t.Id);

        return await this.contexto.Tomas.AnyAsync(t => tratamentos.Contains(t.TratamentoId), cancelamento).ConfigureAwait(false)
            || await this.contexto.ExecucoesTarefa.AnyAsync(e => tarefas.Contains(e.TarefaId), cancelamento).ConfigureAwait(false)
            || await this.contexto.Ocorrencias.AnyAsync(o => o.FichaId == fichaId, cancelamento).ConfigureAwait(false)
            || await this.contexto.Notas.AnyAsync(n => n.FichaId == fichaId, cancelamento).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> EliminarFuturasPorConfirmarAsync(Guid fichaId, DateTime aPartirDeUtc, CancellationToken cancelamento)
    {
        var tratamentos = this.contexto.Tratamentos.Where(t => t.FichaId == fichaId).Select(t => t.Id);
        var tarefas = this.contexto.Tarefas.Where(t => t.FichaId == fichaId).Select(t => t.Id);

        var tomas = await this.contexto.Tomas
            .Where(t => tratamentos.Contains(t.TratamentoId) && t.Estado == EstadoToma.Pendente && t.PrevistaEmUtc > aPartirDeUtc)
            .ExecuteDeleteAsync(cancelamento)
            .ConfigureAwait(false);
        var execucoes = await this.contexto.ExecucoesTarefa
            .Where(e => tarefas.Contains(e.TarefaId) && e.Estado == EstadoExecucaoTarefa.PorConfirmar && e.PrevistaEmUtc > aPartirDeUtc)
            .ExecuteDeleteAsync(cancelamento)
            .ConfigureAwait(false);
        return tomas + execucoes;
    }

    /// <inheritdoc/>
    public void Adicionar(Ficha ficha)
    {
        this.contexto.Fichas.Add(ficha);
    }

    /// <inheritdoc/>
    public void Remover(Ficha ficha)
    {
        this.contexto.Fichas.Remove(ficha);
    }
}
