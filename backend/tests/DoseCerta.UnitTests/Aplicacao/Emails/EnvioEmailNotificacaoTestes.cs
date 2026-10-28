// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Emails;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Emails;

/// <summary>
/// Testes unitários do envio e dos reenvios dos e-mails de notificação no <see cref="EnvioEmailService"/> (RF-118, RF-119,
/// RF-141, RNF-018), incluindo as notificações criadas sem e-mail por outras funcionalidades.
/// </summary>
public class EnvioEmailNotificacaoTestes
{
    private static readonly DateTime Criada = FichasDeTeste.Agora;

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly EmailSenderFalso email = new();
    private readonly EnvioEmailService servico;
    private readonly Utilizador cuidador = new("Rui Fictício", "rui.ficticio@example.com", "hash", Criada);
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailNotificacaoTestes"/> class.
    /// </summary>
    public EnvioEmailNotificacaoTestes()
    {
        this.servico = ServicosDeTeste.EnvioEmailService(this.repositorios, new UnidadeDeTrabalhoEmMemoria(), new RelogioFixo(Criada), this.email);
        this.repositorios.Utilizadores.Add(this.cuidador);
        this.ficha = FichasDeTeste.DeTerceiro(this.cuidador.Id);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-141, RNF-018: o e-mail de uma notificação de atraso é submetido logo no primeiro ciclo, com o assunto do tipo e um corpo
    /// com a mensagem e a data em hora de Lisboa, e fica enviado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailDaNotificacaoEEnviado()
    {
        var notificacao = this.NovaNotificacao(TipoNotificacao.TomaEmAtraso);
        var envio = this.servico.RegistarDeNotificacao(notificacao, this.cuidador.Email, Criada);

        Assert.Equal(1, await this.servico.ProcessarPendentesAsync(Criada, CancellationToken.None));

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        var (destino, assunto, corpo) = Assert.Single(this.email.Enviados);
        Assert.Equal(this.cuidador.Email, destino);
        Assert.Equal(TextosNotificacao.AssuntoTomaEmAtraso, assunto);
        Assert.Equal(TextosNotificacao.CorpoEmail(notificacao.Mensagem, "21/10/2026 10:00"), corpo);
    }

    /// <summary>
    /// RF-118 CA1 e RF-119: com o serviço sempre em falha, há a tentativa inicial e 3 reenvios de 5 em 5 minutos, e o e-mail fica
    /// não entregue; a notificação mantém-se (RF-141).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaReenviaTresVezesEFicaNaoEntregue()
    {
        var notificacao = this.NovaNotificacao(TipoNotificacao.TomaGraveFamiliares);
        var envio = this.servico.RegistarDeNotificacao(notificacao, this.cuidador.Email, Criada);
        this.email.Falhar = true;

        foreach (var minutos in new[] { 0, 4, 5, 10, 15, 20 })
        {
            await this.servico.ProcessarPendentesAsync(Criada.AddMinutes(minutos), CancellationToken.None);
        }

        Assert.Equal(4, this.email.Tentativas);
        Assert.Equal(EstadoEnvioEmail.NaoEntregue, envio.Estado);
        Assert.Equal(3, envio.Reenvios);
        Assert.Equal(Criada.AddMinutes(15), envio.UltimaTentativaEmUtc);
        Assert.Contains(notificacao, this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RF-118 CA2: o serviço volta antes do segundo reenvio; o e-mail é entregue e não há mais tentativas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ServicoQueVoltaEntregaNoReenvioSeguinte()
    {
        var envio = this.servico.RegistarDeNotificacao(this.NovaNotificacao(TipoNotificacao.TomaEmAtraso), this.cuidador.Email, Criada);
        this.email.Falhar = true;
        await this.servico.ProcessarPendentesAsync(Criada, CancellationToken.None);
        await this.servico.ProcessarPendentesAsync(Criada.AddMinutes(5), CancellationToken.None);
        this.email.Falhar = false;

        await this.servico.ProcessarPendentesAsync(Criada.AddMinutes(10), CancellationToken.None);
        await this.servico.ProcessarPendentesAsync(Criada.AddMinutes(15), CancellationToken.None);

        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Equal(2, envio.Reenvios);
        Assert.Equal(3, this.email.Tentativas);
    }

    /// <summary>
    /// RF-141: uma notificação criada só na aplicação, por exemplo uma <c>NovaOcorrencia</c> de outra funcionalidade, ganha o
    /// e-mail no ciclo seguinte, para o endereço atual do destinatário, e é enviada; não volta a ser registada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NotificacaoSemEmailGanhaEmailEEEnviada()
    {
        var notificacao = this.NovaNotificacao(TipoNotificacao.NovaOcorrencia);

        await this.servico.ProcessarPendentesAsync(Criada.AddSeconds(30), CancellationToken.None);
        Assert.Equal(0, await this.servico.RegistarEmFaltaAsync(Criada.AddMinutes(1), CancellationToken.None));

        var envio = Assert.Single(this.repositorios.EnviosEmail);
        Assert.Equal(notificacao.Id, envio.NotificacaoId);
        Assert.Equal(this.cuidador.Email, envio.EnderecoDestino);
        Assert.Equal(TextosNotificacao.AssuntoGenerico, envio.Assunto);
        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Equal(Criada.AddSeconds(30), envio.PrimeiraTentativaEmUtc);
        Assert.StartsWith(notificacao.Mensagem, Assert.Single(this.email.Enviados).Corpo, StringComparison.Ordinal);
    }

    /// <summary>
    /// Um e-mail de notificação cuja notificação desapareceu conta como falha, sem parar o processamento.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailSemNotificacaoContaComoFalha()
    {
        var notificacao = this.NovaNotificacao(TipoNotificacao.TomaEmAtraso);
        var envio = this.servico.RegistarDeNotificacao(notificacao, this.cuidador.Email, Criada);
        this.repositorios.Notificacoes.Remove(notificacao);

        await this.servico.ProcessarPendentesAsync(Criada, CancellationToken.None);

        Assert.Equal(EstadoEnvioEmail.Pendente, envio.Estado);
        Assert.Equal(Criada.AddMinutes(5), envio.ProximaTentativaEmUtc);
        Assert.Empty(this.email.Enviados);
    }

    private Notificacao NovaNotificacao(TipoNotificacao tipo)
    {
        var referencia = tipo == TipoNotificacao.NovaOcorrencia ? TipoReferencia.Ocorrencia : TipoReferencia.Toma;
        var notificacao = new Notificacao(
            this.cuidador.Id, this.ficha.Id, tipo, "A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.", referencia, Guid.NewGuid(), Criada, Criada);
        this.repositorios.Notificacoes.Add(notificacao);
        return notificacao;
    }
}
