// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Collections.Concurrent;
using DoseCerta.Application.Abstracoes;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Serviço de e-mail controlado pelos testes de integração, em vez do SMTP: regista cada tentativa e pode simular falhas (RF-136).
/// </summary>
public sealed class EmailSenderControlado : IEmailSender
{
    /// <summary>
    /// Gets as tentativas recebidas, com o resultado.
    /// </summary>
    public ConcurrentQueue<(string Destino, string Assunto, string Corpo, bool Aceite)> Tentativas { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether o serviço recusa todas as mensagens.
    /// </summary>
    public bool Falhar { get; set; }

    /// <inheritdoc/>
    public Task<bool> EnviarAsync(string destino, string assunto, string corpo, CancellationToken cancelamento)
    {
        var aceite = !this.Falhar;
        this.Tentativas.Enqueue((destino, assunto, corpo, aceite));
        return Task.FromResult(aceite);
    }
}
