// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Emails;

/// <summary>
/// Regista os e-mails a enviar, separados do convite e da notificação que os originam, para que uma falha de envio nunca desfaça
/// a operação (RF-135, RF-141, DM-13). Nesta tarefa só trata dos e-mails de convite (F004); os e-mails das restantes notificações
/// são da F011.
/// </summary>
public sealed class EnvioEmailService
{
    private readonly IEnvioEmailRepositorio envios;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailService"/> class.
    /// </summary>
    /// <param name="envios">Repositório dos e-mails a enviar.</param>
    public EnvioEmailService(IEnvioEmailRepositorio envios)
    {
        this.envios = envios;
    }

    /// <summary>
    /// Regista, na transação do convite, o e-mail de convite como pendente, com a primeira tentativa devida de imediato (RF-041, RNF-018).
    /// </summary>
    /// <param name="convite">Convite criado.</param>
    /// <param name="notificacaoId">Notificação <c>ConviteRecebido</c> do mesmo convite, se o e-mail já tiver conta.</param>
    /// <param name="agoraUtc">Instante da criação, em UTC.</param>
    /// <returns>O e-mail registado.</returns>
    public EnvioEmail RegistarDeConvite(Convite convite, Guid? notificacaoId, DateTime agoraUtc)
    {
        ArgumentNullException.ThrowIfNull(convite);
        var envio = new EnvioEmail(notificacaoId, convite.Id, convite.EmailConvidado, TextosConvite.Assunto, agoraUtc);
        this.envios.Adicionar(envio);
        return envio;
    }
}
