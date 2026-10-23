// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Emails;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Emails;

/// <summary>
/// Testes unitários do envio e dos reenvios dos e-mails de convite no <see cref="EnvioEmailService"/> (RF-118, RF-119, RF-135, RF-136).
/// </summary>
public class EnvioEmailServiceTestes
{
    private static readonly DateTime Criado = FichasDeTeste.Agora;

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly EmailSenderFalso email = new();
    private readonly EnvioEmailService servico;
    private readonly Ficha ficha;
    private readonly Guid cuidadorId;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvioEmailServiceTestes"/> class.
    /// </summary>
    public EnvioEmailServiceTestes()
    {
        this.servico = ServicosDeTeste.EnvioEmailService(this.repositorios, this.unidadeDeTrabalho, new RelogioFixo(Criado), this.email);
        var cuidador = new Utilizador("Zé Cuidador", "ze@exemplo.pt", "hash-ficticio", Criado);
        this.repositorios.Utilizadores.Add(cuidador);
        this.cuidadorId = cuidador.Id;
        this.ficha = FichasDeTeste.DeTerceiro(cuidador.Id);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-041, RNF-018, RNF-029: o e-mail devido é submetido ao serviço de envio, com o assunto e um corpo que indica quem convidou,
    /// a ficha, o papel e a data em hora de Lisboa, e fica enviado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EmailDevidoEEnviado()
    {
        var (convite, envio) = this.NovoConvite("rui@exemplo.pt");

        var tentados = await this.servico.ProcessarPendentesAsync(Criado, CancellationToken.None);

        Assert.Equal(1, tentados);
        Assert.Equal(EstadoEnvioEmail.Enviado, envio.Estado);
        Assert.Equal(Criado, envio.PrimeiraTentativaEmUtc);
        var (destino, assunto, corpo) = Assert.Single(this.email.Enviados);
        Assert.Equal("rui@exemplo.pt", destino);
        Assert.Equal(TextosConvite.Assunto, assunto);
        Assert.Equal(
            "Recebeu um convite de Zé Cuidador para a ficha de Maria Fictícia Santos no DoseCerta, como Familiar.\n\n"
            + "Para aceitar ou recusar, inicie sessão no DoseCerta com este endereço de e-mail e abra a área de convites. "
            + "Se ainda não tem conta, crie uma com este endereço: o convite aparece na área de convites.\n\n"
            + "Convite feito em 21/10/2026 10:00.",
            corpo);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-136 CA1 e CA2, RF-135 CA1: com o serviço de envio sempre em falha, há uma tentativa e três reenvios, de 5 em 5 minutos;
    /// depois o e-mail fica não entregue, não há mais tentativas e o convite continua pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NoMaximoTresReenviosDepoisNaoEntregue()
    {
        var (convite, envio) = this.NovoConvite("rui@exemplo.pt");
        this.email.Falhar = true;

        foreach (var minutos in new[] { 0, 5, 10, 15, 20, 60 })
        {
            await this.servico.ProcessarPendentesAsync(Criado.AddMinutes(minutos), CancellationToken.None);
        }

        Assert.Equal(4, this.email.Tentativas);
        Assert.Equal(EstadoEnvioEmail.NaoEntregue, envio.Estado);
        Assert.Equal(3, envio.Reenvios);
        Assert.Equal(Criado.AddMinutes(15), envio.UltimaTentativaEmUtc);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
    }

    /// <summary>
    /// RF-136: depois de uma falha, não há nova tentativa antes de passarem 5 minutos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AntesDosCincoMinutosNaoReenvia()
    {
        var (_, envio) = this.NovoConvite("rui@exemplo.pt");
        this.email.Falhar = true;
        await this.servico.ProcessarPendentesAsync(Criado, CancellationToken.None);

        var tentados = await this.servico.ProcessarPendentesAsync(Criado.AddMinutes(4), CancellationToken.None);

        Assert.Equal(0, tentados);
        Assert.Equal(1, this.email.Tentativas);
        Assert.Equal(Criado.AddMinutes(5), envio.ProximaTentativaEmUtc);
    }

    /// <summary>
    /// Uma exceção do serviço de envio conta como falha (reenvio agendado), sem impedir o envio dos outros e-mails devidos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ExcecaoDoEnvioContaComoFalha()
    {
        var (_, primeiro) = this.NovoConvite("rui@exemplo.pt");
        var (_, segundo) = this.NovoConvite("rita@exemplo.pt");
        this.email.LancarExcecao = true;

        var tentados = await this.servico.ProcessarPendentesAsync(Criado, CancellationToken.None);

        Assert.Equal(2, tentados);
        Assert.All(new[] { primeiro, segundo }, e =>
        {
            Assert.Equal(EstadoEnvioEmail.Pendente, e.Estado);
            Assert.Equal(Criado.AddMinutes(5), e.ProximaTentativaEmUtc);
        });
    }

    private (Convite Convite, EnvioEmail Envio) NovoConvite(string emailConvidado)
    {
        var convite = new Convite(this.ficha.Id, emailConvidado, Papel.Familiar, paraPrincipal: false, this.cuidadorId, Criado);
        this.repositorios.Convites.Add(convite);
        var envio = this.servico.RegistarDeConvite(convite, null, Criado);
        return (convite, envio);
    }
}
