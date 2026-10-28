// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Ocorrencias;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositórios em memória das ocorrências e das notas partilhadas (F009). Como na base de dados, só devolvem os registos cujo
/// autor tem conta, do registo mais recente para o mais antigo.
/// </summary>
public partial class RepositoriosEmMemoria : IOcorrenciaRepositorio, INotaRepositorio
{
    /// <summary>
    /// Gets as ocorrências registadas.
    /// </summary>
    public List<Ocorrencia> Ocorrencias { get; } = new();

    /// <summary>
    /// Gets as notas registadas.
    /// </summary>
    public List<Nota> Notas { get; } = new();

    /// <inheritdoc/>
    Task<IReadOnlyList<RegistoPartilhado>> IOcorrenciaRepositorio.DaFichaAsync(
        Guid fichaId,
        DateTime? deUtc,
        DateTime? ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<RegistoPartilhado> lista = this.Ocorrencias
            .Where(o => o.FichaId == fichaId && NoIntervalo(o.RegistadaEmUtc, deUtc, ateUtc))
            .Join(this.Utilizadores, o => o.AutorId, u => u.Id, (o, u) => new RegistoPartilhado(
                o.Id,
                o.FichaId,
                TipoRegisto.Ocorrencia,
                o.Descricao,
                o.AutorId,
                u.Nome,
                o.OcorreuEmUtc,
                o.RegistadaEmUtc))
            .OrderByDescending(r => r.RegistadaEmUtc)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    Task<IReadOnlyList<RegistoPartilhado>> INotaRepositorio.DaFichaAsync(
        Guid fichaId,
        DateTime? deUtc,
        DateTime? ateUtc,
        CancellationToken cancelamento)
    {
        IReadOnlyList<RegistoPartilhado> lista = this.Notas
            .Where(n => n.FichaId == fichaId && NoIntervalo(n.RegistadaEmUtc, deUtc, ateUtc))
            .Join(this.Utilizadores, n => n.AutorId, u => u.Id, (n, u) => new RegistoPartilhado(
                n.Id,
                n.FichaId,
                TipoRegisto.Nota,
                n.Texto,
                n.AutorId,
                u.Nome,
                null,
                n.RegistadaEmUtc))
            .OrderByDescending(r => r.RegistadaEmUtc)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public void Adicionar(Ocorrencia ocorrencia)
    {
        this.Ocorrencias.Add(ocorrencia);
    }

    /// <inheritdoc/>
    public void Adicionar(Nota nota)
    {
        this.Notas.Add(nota);
    }

    private static bool NoIntervalo(DateTime instanteUtc, DateTime? deUtc, DateTime? ateUtc)
    {
        return (deUtc is null || instanteUtc >= deUtc) && (ateUtc is null || instanteUtc < ateUtc);
    }
}
