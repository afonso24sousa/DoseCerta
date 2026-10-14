// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;

namespace DoseCerta.Api.Arranque;

/// <summary>
/// Comando de linha de comandos que envia uma mensagem de teste pelo serviço de e-mail configurado,
/// para demonstrar a configuração do envio sem expor um endpoint na API.
/// Utilização: <c>dotnet run --project src/DoseCerta.Api -- --enviar-email-teste destino@exemplo.pt</c>.
/// </summary>
public static class ComandoEmailTeste
{
    /// <summary>
    /// Argumento que ativa o comando.
    /// </summary>
    public const string Argumento = "--enviar-email-teste";

    /// <summary>
    /// Indica se os argumentos pedem o envio de um e-mail de teste e obtém o destino.
    /// </summary>
    /// <param name="args">Argumentos da linha de comandos.</param>
    /// <param name="destino">Endereço de destino, quando o comando é pedido.</param>
    /// <returns><see langword="true"/> se o comando foi pedido com um destino.</returns>
    public static bool Pedido(string[] args, out string destino)
    {
        ArgumentNullException.ThrowIfNull(args);
        var indice = Array.IndexOf(args, Argumento);
        destino = indice >= 0 && indice + 1 < args.Length ? args[indice + 1] : string.Empty;
        return destino.Length > 0;
    }

    /// <summary>
    /// Envia a mensagem de teste e devolve o código de saída do processo.
    /// </summary>
    /// <param name="servicos">Fornecedor de serviços da aplicação.</param>
    /// <param name="destino">Endereço de destino.</param>
    /// <returns>0 se o serviço aceitou o e-mail; 1 caso contrário.</returns>
    public static async Task<int> ExecutarAsync(IServiceProvider servicos, string destino)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        await using var ambito = servicos.CreateAsyncScope();
        var envio = ambito.ServiceProvider.GetRequiredService<IEmailSender>();
        var relogio = ambito.ServiceProvider.GetRequiredService<IRelogio>();
        var instante = relogio.Calendario().FormatarDataHora(relogio.AgoraUtc());

        var aceite = await envio.EnviarAsync(
            destino,
            "DoseCerta: mensagem de teste",
            $"Mensagem de teste do backend do DoseCerta, enviada em {instante} (hora de Lisboa).",
            CancellationToken.None).ConfigureAwait(false);

        return aceite ? 0 : 1;
    }
}
