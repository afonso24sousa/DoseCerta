// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Serviço de e-mail falso: regista as mensagens e pode simular falhas (RF-136).
/// </summary>
public sealed class EmailSenderFalso : IEmailSender
{
    /// <summary>
    /// Gets as mensagens aceites.
    /// </summary>
    public List<(string Destino, string Assunto, string Corpo)> Enviados { get; } = new();

    /// <summary>
    /// Gets o número total de tentativas recebidas.
    /// </summary>
    public int Tentativas { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether o serviço recusa todas as mensagens.
    /// </summary>
    public bool Falhar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether o serviço lança uma exceção de E/S em vez de devolver falso.
    /// </summary>
    public bool LancarExcecao { get; set; }

    /// <inheritdoc/>
    public Task<bool> EnviarAsync(string destino, string assunto, string corpo, CancellationToken cancelamento)
    {
        this.Tentativas++;
        if (this.LancarExcecao)
        {
            throw new IOException("Falha simulada na ligação ao serviço de e-mail.");
        }

        if (this.Falhar)
        {
            return Task.FromResult(false);
        }

        this.Enviados.Add((destino, assunto, corpo));
        return Task.FromResult(true);
    }
}
