// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Repositórios em memória das notificações criadas e dos e-mails a enviar (F004, convites).
/// </summary>
public partial class RepositoriosEmMemoria : IEnvioEmailRepositorio
{
    /// <summary>
    /// Gets as notificações criadas.
    /// </summary>
    public List<Notificacao> Notificacoes { get; } = new();

    /// <summary>
    /// Gets os e-mails registados.
    /// </summary>
    public List<EnvioEmail> EnviosEmail { get; } = new();

    /// <inheritdoc/>
    public void Adicionar(Notificacao notificacao)
    {
        this.Notificacoes.Add(notificacao);
    }

    /// <inheritdoc/>
    public void Adicionar(EnvioEmail envio)
    {
        this.EnviosEmail.Add(envio);
    }
}
