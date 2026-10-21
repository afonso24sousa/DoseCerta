// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Pessoas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositórios em memória das fichas, das listas fechadas, dos documentos e das notificações (F002).
/// </summary>
public partial class RepositoriosEmMemoria : IFichaRepositorio, IDominiosRepositorio, IDocumentoRepositorio, INotificacaoRepositorio
{
    /// <summary>
    /// Gets as fichas guardadas.
    /// </summary>
    public List<Ficha> Fichas { get; } = new();

    /// <summary>
    /// Gets a lista fechada de condições de saúde.
    /// </summary>
    public List<CondicaoSaude> Condicoes { get; } = new() { new CondicaoSaude(1, "Diabetes"), new CondicaoSaude(2, "Hipertensão arterial") };

    /// <summary>
    /// Gets a lista fechada de alergias.
    /// </summary>
    public List<Alergia> ListaAlergias { get; } = new() { new Alergia(1, "Medicamentos"), new Alergia(2, "Alimentos") };

    /// <summary>
    /// Gets a lista fechada de graus de autonomia.
    /// </summary>
    public List<GrauAutonomia> Graus { get; } = new() { new GrauAutonomia(1, "Autónomo", 1), new GrauAutonomia(2, "Autonomia parcial", 2) };

    /// <summary>
    /// Gets os metadados dos documentos guardados.
    /// </summary>
    public List<Documento> Documentos { get; } = new();

    /// <summary>
    /// Gets as fichas simuladas com medicação registada.
    /// </summary>
    public HashSet<Guid> FichasComMedicacao { get; } = new();

    /// <summary>
    /// Gets as fichas simuladas com tarefas registadas.
    /// </summary>
    public HashSet<Guid> FichasComTarefas { get; } = new();

    /// <summary>
    /// Gets as fichas simuladas com histórico (tomas, execuções, ocorrências ou notas).
    /// </summary>
    public HashSet<Guid> FichasComHistorico { get; } = new();

    /// <summary>
    /// Gets os pedidos de remoção das tomas e execuções futuras (ficha e instante).
    /// </summary>
    public List<(Guid FichaId, DateTime APartirDeUtc)> FuturasEliminadas { get; } = new();

    /// <summary>
    /// Gets as fichas cujas notificações foram apagadas.
    /// </summary>
    public List<Guid> NotificacoesEliminadas { get; } = new();

    /// <inheritdoc/>
    public Task<Ficha?> ObterCompletaAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Fichas.FirstOrDefault(f => f.Id == id));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PessoaAssociada>> ListarPessoasAsync(Guid fichaId, CancellationToken cancelamento)
    {
        var associacoes = this.Fichas.FirstOrDefault(f => f.Id == fichaId)?.Associacoes ?? Array.Empty<Associacao>();
        IReadOnlyList<PessoaAssociada> pessoas = associacoes
            .Join(this.Utilizadores, a => a.UtilizadorId, u => u.Id, (a, u) => new PessoaAssociada(u.Id, u.Nome, u.Email, a.Papel, a.EPrincipal, a.EProvisorio))
            .OrderByDescending(p => p.Principal)
            .ThenBy(p => p.Nome, StringComparer.Ordinal)
            .ThenBy(p => p.UtilizadorId)
            .ToList();
        return Task.FromResult(pessoas);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Ficha>> ListarDoUtilizadorAsync(Guid utilizadorId, EstadoFicha? estado, CancellationToken cancelamento)
    {
        IReadOnlyList<Ficha> lista = this.Fichas
            .Where(f => f.ObterAssociacao(utilizadorId) is not null && (estado is null || f.Estado == estado))
            .OrderBy(f => f.NomeCompleto, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(lista);
    }

    /// <inheritdoc/>
    public Task<bool> ExistePropriaDeAsync(Guid utilizadorId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Fichas.Any(f => f.UtilizadorProprioId == utilizadorId));
    }

    /// <inheritdoc/>
    public Task<bool> TemMedicacaoAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.FichasComMedicacao.Contains(fichaId));
    }

    /// <inheritdoc/>
    public Task<bool> TemTarefasAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.FichasComTarefas.Contains(fichaId));
    }

    /// <inheritdoc/>
    public Task<bool> TemHistoricoAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.FichasComHistorico.Contains(fichaId));
    }

    /// <inheritdoc/>
    public Task<int> EliminarFuturasPorConfirmarAsync(Guid fichaId, DateTime aPartirDeUtc, CancellationToken cancelamento)
    {
        this.FuturasEliminadas.Add((fichaId, aPartirDeUtc));
        return Task.FromResult(0);
    }

    /// <inheritdoc/>
    public void Adicionar(Ficha ficha)
    {
        this.Fichas.Add(ficha);
    }

    /// <inheritdoc/>
    public void Remover(Ficha ficha)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        this.Fichas.Remove(ficha);
        this.Documentos.RemoveAll(d => d.FichaId == ficha.Id);
        this.Convites.RemoveAll(c => c.FichaId == ficha.Id);
    }

    /// <inheritdoc/>
    public void Adicionar(Convite convite)
    {
        this.Convites.Add(convite);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CondicaoSaude>> ListarCondicoesAsync(CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<CondicaoSaude>>(this.Condicoes.ToList());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Alergia>> ListarAlergiasAsync(CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<Alergia>>(this.ListaAlergias.ToList());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<GrauAutonomia>> ListarGrausAutonomiaAsync(CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<GrauAutonomia>>(this.Graus.OrderBy(g => g.Ordem).ToList());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CondicaoSaude>> CondicoesPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<CondicaoSaude>>(this.Condicoes.Where(c => ids.Contains(c.Id)).ToList());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Alergia>> AlergiasPorIdsAsync(IReadOnlyCollection<short> ids, CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<Alergia>>(this.ListaAlergias.Where(a => ids.Contains(a.Id)).ToList());
    }

    /// <inheritdoc/>
    public Task<GrauAutonomia?> GrauPorIdAsync(short id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Graus.FirstOrDefault(g => g.Id == id));
    }

    /// <inheritdoc/>
    Task<Documento?> IDocumentoRepositorio.ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Documentos.FirstOrDefault(d => d.Id == id));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Documento>> DaFichaAsync(Guid fichaId, CancellationToken cancelamento)
    {
        return Task.FromResult<IReadOnlyList<Documento>>(this.Documentos.Where(d => d.FichaId == fichaId).ToList());
    }

    /// <inheritdoc/>
    public void Adicionar(Documento documento)
    {
        this.Documentos.Add(documento);
    }

    /// <inheritdoc/>
    public Task<int> EliminarDaFichaAsync(Guid fichaId, CancellationToken cancelamento)
    {
        this.NotificacoesEliminadas.Add(fichaId);
        return Task.FromResult(0);
    }
}
