// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Armazenamento em memória que implementa os repositórios usados pelos serviços de aplicação,
/// com a mesma regra de comparação de e-mails sem distinção de maiúsculas da base de dados.
/// </summary>
public partial class RepositoriosEmMemoria : IUtilizadorRepositorio, ISessaoRevogadaRepositorio, IConviteRepositorio
{
    /// <summary>
    /// Gets as contas guardadas.
    /// </summary>
    public List<Utilizador> Utilizadores { get; } = new();

    /// <summary>
    /// Gets as credenciais terminadas.
    /// </summary>
    public List<SessaoRevogada> Sessoes { get; } = new();

    /// <summary>
    /// Gets os convites guardados.
    /// </summary>
    public List<Convite> Convites { get; } = new();

    /// <inheritdoc/>
    public Task<Utilizador?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Utilizadores.FirstOrDefault(u => u.Id == id));
    }

    /// <inheritdoc/>
    public Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Utilizadores.FirstOrDefault(u => MesmoEmail(u.Email, email)));
    }

    /// <inheritdoc/>
    public Task<bool> ExisteEmailAsync(string email, Guid? excetoId, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Utilizadores.Any(u => MesmoEmail(u.Email, email) && u.Id != excetoId));
    }

    /// <inheritdoc/>
    public void Adicionar(Utilizador utilizador)
    {
        this.Utilizadores.Add(utilizador);
    }

    /// <inheritdoc/>
    public Task<bool> EstaRevogadaAsync(Guid jti, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Sessoes.Any(s => s.Jti == jti));
    }

    /// <inheritdoc/>
    public void Adicionar(SessaoRevogada sessao)
    {
        this.Sessoes.Add(sessao);
    }

    /// <inheritdoc/>
    public Task<int> EliminarExpiradasAsync(DateTime agoraUtc, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Sessoes.RemoveAll(s => s.ExpiraEmUtc <= agoraUtc));
    }

    /// <inheritdoc/>
    public Task<int> ContarPendentesPorEmailAsync(string email, CancellationToken cancelamento)
    {
        return Task.FromResult(this.Convites.Count(c => MesmoEmail(c.EmailConvidado, email) && c.Estado == EstadoConvite.Pendente));
    }

    /// <summary>
    /// Compara dois e-mails sem distinção de maiúsculas, como a collation CI_AI da base de dados.
    /// </summary>
    /// <param name="a">Primeiro e-mail.</param>
    /// <param name="b">Segundo e-mail.</param>
    /// <returns><see langword="true"/> se forem iguais.</returns>
    protected static bool MesmoEmail(string a, string b)
    {
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
