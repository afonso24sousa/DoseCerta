// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Convites;
using DoseCerta.Application.Emails;
using DoseCerta.Application.Fichas;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Construção, num só sítio, dos serviços de aplicação da F004 sobre os repositórios em memória.
/// </summary>
public static class ServicosDeTeste
{
    /// <summary>
    /// Cria o serviço dos convites com os repositórios em memória.
    /// </summary>
    /// <param name="repositorios">Repositórios em memória.</param>
    /// <param name="unidadeDeTrabalho">Unidade de trabalho dos testes.</param>
    /// <param name="relogio">Relógio dos testes.</param>
    /// <returns>O serviço dos convites.</returns>
    public static ConviteService ConviteService(RepositoriosEmMemoria repositorios, IUnidadeDeTrabalho unidadeDeTrabalho, IRelogio relogio)
    {
        return new ConviteService(
            repositorios,
            repositorios,
            repositorios,
            repositorios,
            EnvioEmailService(repositorios, unidadeDeTrabalho, relogio, new EmailSenderFalso()),
            new AutorizacaoFichaService(repositorios),
            unidadeDeTrabalho,
            relogio);
    }

    /// <summary>
    /// Cria o serviço dos e-mails com os repositórios em memória.
    /// </summary>
    /// <param name="repositorios">Repositórios em memória.</param>
    /// <param name="unidadeDeTrabalho">Unidade de trabalho dos testes.</param>
    /// <param name="relogio">Relógio dos testes.</param>
    /// <param name="emailSender">Serviço de e-mail falso.</param>
    /// <returns>O serviço dos e-mails.</returns>
    public static EnvioEmailService EnvioEmailService(
        RepositoriosEmMemoria repositorios,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio,
        IEmailSender emailSender)
    {
        return new EnvioEmailService(repositorios, repositorios, emailSender, unidadeDeTrabalho, relogio);
    }
}
