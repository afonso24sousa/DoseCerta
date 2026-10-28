// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Tomas;

/// <summary>
/// Testes unitários dos passos 2 e 3 do processo periódico em <see cref="AtrasoTomaService"/>: tolerância de cada grau e
/// destinatários das notificações de atraso (RF-110 a RF-113, RF-141, RF-142, RNF-043; TR-05, TR-07 e TR-10). A ficha foi criada
/// a 21 de outubro de 2026, 10:00 de Lisboa, e a toma está prevista para as 11:00 de Lisboa (10:00 UTC) do mesmo dia.
/// </summary>
public class AtrasoTomaServiceTestes
{
    private static readonly DateTime Prevista = FichasDeTeste.Agora.AddHours(1);
    private static readonly string Mensagem = "A toma de Insulina de Maria Fictícia Santos das 11:00 não foi confirmada.";

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Prevista);
    private readonly AtrasoTomaService servico;
    private readonly Utilizador principal = new("Ana Fictícia", "ana.ficticia@example.com", "hash", FichasDeTeste.Agora);
    private readonly Utilizador cuidador = new("Rui Fictício", "rui.ficticio@example.com", "hash", FichasDeTeste.Agora);
    private readonly Utilizador familiar = new("Joana Fictícia", "joana.ficticia@example.com", "hash", FichasDeTeste.Agora);
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtrasoTomaServiceTestes"/> class.
    /// </summary>
    public AtrasoTomaServiceTestes()
    {
        this.repositorios.Utilizadores.AddRange(new[] { this.principal, this.cuidador, this.familiar });
        var envioEmail = ServicosDeTeste.EnvioEmailService(this.repositorios, this.unidadeDeTrabalho, this.relogio, new EmailSenderFalso());
        var notificacoes = new NotificacaoService(this.repositorios, this.repositorios, envioEmail, this.relogio);
        this.servico = new AtrasoTomaService(this.repositorios, this.repositorios, this.repositorios, notificacoes, this.unidadeDeTrabalho, this.relogio);

        this.ficha = FichasDeTeste.DeTerceiro(this.principal.Id);
        this.ficha.Associar(this.cuidador.Id, Papel.Cuidador, FichasDeTeste.Agora);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, FichasDeTeste.Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-110 CA1 e CA3: numa toma Leve, às T + 2 h ainda não há notificação; um segundo depois, cada Cuidador (incluindo o
    /// principal) recebe uma notificação <c>TomaEmAtraso</c> com a ficha, o medicamento e a hora prevista, e o respetivo e-mail;
    /// o Familiar nunca é notificado e a toma continua <c>Pendente</c> (RF-066).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task LeveNotificaSoOsCuidadoresDepoisDasDuasHoras()
    {
        var toma = this.NovaToma(GrauImportancia.Leve);

        Assert.Equal(0, await this.servico.NotificarCuidadoresAsync(Prevista.AddHours(2), CancellationToken.None));
        Assert.Empty(this.repositorios.Notificacoes);

        var depois = Prevista.AddHours(2).AddSeconds(1);
        Assert.Equal(1, await this.servico.NotificarCuidadoresAsync(depois, CancellationToken.None));
        Assert.Equal(0, await this.servico.NotificarFamiliaresAsync(Prevista.AddHours(3), CancellationToken.None));

        Assert.Equal(new[] { this.principal.Id, this.cuidador.Id }.Order(), this.repositorios.Notificacoes.Select(n => n.DestinatarioId).Order());
        Assert.All(this.repositorios.Notificacoes, n =>
        {
            Assert.Equal(TipoNotificacao.TomaEmAtraso, n.Tipo);
            Assert.Equal(TipoReferencia.Toma, n.ReferenciaTipo);
            Assert.Equal(toma.Id, n.ReferenciaId);
            Assert.Equal(this.ficha.Id, n.FichaId);
            Assert.Equal(Prevista.AddHours(2), n.InstanteLimiteUtc);
            Assert.Equal(depois, n.CriadaEmUtc);
            Assert.Equal(Mensagem, n.Mensagem);
        });
        Assert.Equal(
            new[] { this.principal.Email, this.cuidador.Email }.Order(),
            this.repositorios.EnviosEmail.Select(e => e.EnderecoDestino).Order());
        Assert.All(this.repositorios.EnviosEmail, e =>
        {
            Assert.Equal(TextosNotificacao.AssuntoTomaEmAtraso, e.Assunto);
            Assert.Equal(EstadoEnvioEmail.Pendente, e.Estado);
            Assert.Null(e.ConviteId);
            Assert.Contains(this.repositorios.Notificacoes, n => n.Id == e.NotificacaoId);
        });
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal(depois, toma.CuidadoresNotificadosEmUtc);
    }

    /// <summary>
    /// RF-111 e RF-112: a tolerância dos Cuidadores é a do grau da toma (Moderado 1 h, Grave 30 min).
    /// </summary>
    /// <param name="grau">Grau de importância.</param>
    /// <param name="minutos">Tolerância do grau, em minutos.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData(GrauImportancia.Moderado, 60)]
    [InlineData(GrauImportancia.Grave, 30)]
    public async Task ToleranciaDosCuidadoresEADoGrau(GrauImportancia grau, int minutos)
    {
        this.NovaToma(grau);

        Assert.Equal(0, await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(minutos), CancellationToken.None));
        Assert.Equal(1, await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(minutos).AddSeconds(1), CancellationToken.None));
        Assert.Equal(2, this.repositorios.Notificacoes.Count(n => n.Tipo == TipoNotificacao.TomaEmAtraso));
    }

    /// <summary>
    /// RF-112 e RF-113 CA1: numa toma Grave, os Cuidadores são notificados depois dos 30 minutos e o Familiar 30 minutos depois,
    /// com uma notificação <c>TomaGraveFamiliares</c> e o respetivo e-mail; aos 60 minutos exatos ainda não.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GraveNotificaOsFamiliaresTrintaMinutosDepoisDosCuidadores()
    {
        var toma = this.NovaToma(GrauImportancia.Grave);

        await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(31), CancellationToken.None);
        Assert.Equal(0, await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(60), CancellationToken.None));
        Assert.Equal(1, await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(61), CancellationToken.None));

        var doFamiliar = Assert.Single(this.repositorios.Notificacoes, n => n.DestinatarioId == this.familiar.Id);
        Assert.Equal(TipoNotificacao.TomaGraveFamiliares, doFamiliar.Tipo);
        Assert.Equal(Prevista.AddMinutes(60), doFamiliar.InstanteLimiteUtc);
        Assert.Equal(Mensagem, doFamiliar.Mensagem);
        Assert.Equal(2, this.repositorios.Notificacoes.Count(n => n.Tipo == TipoNotificacao.TomaEmAtraso));
        var email = Assert.Single(this.repositorios.EnviosEmail, e => e.NotificacaoId == doFamiliar.Id);
        Assert.Equal(this.familiar.Email, email.EnderecoDestino);
        Assert.Equal(TextosNotificacao.AssuntoTomaGraveFamiliares, email.Assunto);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal(Prevista.AddMinutes(61), toma.FamiliaresNotificadosEmUtc);
    }

    /// <summary>
    /// RF-112 CA2 e RF-113 CA2: uma toma confirmada antes da deteção não gera notificação; confirmada com atraso depois do aviso
    /// aos Cuidadores, não gera a dos Familiares.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaConfirmadaAntesDaDetecaoNaoGeraNotificacao()
    {
        var calendario = this.relogio.Calendario();
        var noPrazo = this.NovaToma(GrauImportancia.Grave);
        noPrazo.Confirmar(this.cuidador.Id, Prevista.AddMinutes(20), calendario);
        var comAtraso = this.NovaToma(GrauImportancia.Grave, Prevista.AddMinutes(5));
        await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(36), CancellationToken.None);
        comAtraso.Confirmar(this.cuidador.Id, Prevista.AddMinutes(50), calendario);

        await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(70), CancellationToken.None);
        Assert.Equal(0, await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(70), CancellationToken.None));

        Assert.DoesNotContain(this.repositorios.Notificacoes, n => n.ReferenciaId == noPrazo.Id);
        Assert.All(this.repositorios.Notificacoes, n => Assert.Equal(TipoNotificacao.TomaEmAtraso, n.Tipo));
        Assert.Equal(2, this.repositorios.Notificacoes.Count);
    }

    /// <summary>
    /// RNF-043: repetir os passos para o mesmo instante, ou mais tarde, não cria notificações nem e-mails duplicados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RepetirOsPassosNaoDuplicaNotificacoes()
    {
        this.NovaToma(GrauImportancia.Grave);

        foreach (var minutos in new[] { 61, 61, 90 })
        {
            await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(minutos), CancellationToken.None);
            await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(minutos), CancellationToken.None);
        }

        Assert.Equal(3, this.repositorios.Notificacoes.Count);
        Assert.Equal(3, this.repositorios.EnviosEmail.Count);
        Assert.Equal(3, this.repositorios.Notificacoes.Select(n => (n.DestinatarioId, n.Tipo)).Distinct().Count());
    }

    /// <summary>
    /// RF-113 CA3: numa ficha sem Familiares não é criada nenhuma notificação para Familiares e a dos Cuidadores mantém-se; a
    /// toma fica marcada para não voltar a ser analisada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task GraveSemFamiliaresSoNotificaOsCuidadores()
    {
        this.ficha.RemoverAcesso(this.familiar.Id);
        var toma = this.NovaToma(GrauImportancia.Grave);

        await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(61), CancellationToken.None);
        await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(61), CancellationToken.None);

        Assert.Equal(2, this.repositorios.Notificacoes.Count);
        Assert.All(this.repositorios.Notificacoes, n => Assert.Equal(TipoNotificacao.TomaEmAtraso, n.Tipo));
        Assert.NotNull(toma.FamiliaresNotificadosEmUtc);
        Assert.Empty(await this.repositorios.PorNotificarFamiliaresAsync(Prevista.AddMinutes(90), CancellationToken.None));
    }

    /// <summary>
    /// RF-112 CA3 e RF-142 CA2: com a ficha arquivada não há notificações; depois de reativada, o instante-limite que ocorreu
    /// durante o arquivo continua sem notificação, mas o que ocorre depois da reativação é notificado (TR-10).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaSuspendeAsNotificacoes()
    {
        var toma = this.NovaToma(GrauImportancia.Grave);
        this.ficha.Arquivar(Prevista.AddMinutes(10));

        Assert.Equal(0, await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(31), CancellationToken.None));
        Assert.Empty(this.repositorios.Notificacoes);

        this.ficha.Reativar(Prevista.AddMinutes(40));
        Assert.Equal(0, await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(41), CancellationToken.None));
        Assert.Equal(1, await this.servico.NotificarFamiliaresAsync(Prevista.AddMinutes(61), CancellationToken.None));

        var doFamiliar = Assert.Single(this.repositorios.Notificacoes);
        Assert.Equal(TipoNotificacao.TomaGraveFamiliares, doFamiliar.Tipo);
        Assert.Null(toma.CuidadoresNotificadosEmUtc);
    }

    /// <summary>
    /// RNF-044: se a toma for confirmada entre a leitura e a gravação (conflito de versão), a transação é desfeita, o passo não
    /// falha e a toma não conta como notificada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConflitoDeVersaoNaoFazFalharOPasso()
    {
        this.NovaToma(GrauImportancia.Moderado);
        this.unidadeDeTrabalho.AntesDeConfirmar = () => throw new ConflitoDeVersaoException("Confirmada entretanto.");

        var notificadas = await this.servico.NotificarCuidadoresAsync(Prevista.AddMinutes(61), CancellationToken.None);

        Assert.Equal(0, notificadas);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    private Tratamento NovoTratamento(GrauImportancia grau)
    {
        var tratamento = new Tratamento(
            this.ficha.Id, "Insulina", "10 UI", grau, new DateOnly(2026, 10, 21), null, new[] { new TimeOnly(11, 0) }, this.principal.Id, FichasDeTeste.Agora);
        this.repositorios.Tratamentos.Add(tratamento);
        return tratamento;
    }

    private Toma NovaToma(GrauImportancia grau, DateTime? previstaUtc = null)
    {
        var tratamento = this.NovoTratamento(grau);
        var toma = new Toma(tratamento.Id, previstaUtc ?? Prevista, "Insulina", "10 UI", grau);
        this.repositorios.Tomas.Add(toma);
        return toma;
    }
}
