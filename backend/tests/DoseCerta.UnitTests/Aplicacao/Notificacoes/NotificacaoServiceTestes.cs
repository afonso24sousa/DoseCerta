// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Notificacoes;

/// <summary>
/// Testes unitários do <see cref="NotificacaoService"/>: destinatários por papel, ficha arquivada e histórico de notificações
/// de OP-51 (RF-113 CA3, RF-119, RF-140, RF-142).
/// </summary>
public class NotificacaoServiceTestes
{
    private static readonly DateTime Agora = FichasDeTeste.Agora;

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly NotificacaoService servico;
    private readonly Utilizador principal = new("Ana Fictícia", "ana.ficticia@example.com", "hash", Agora);
    private readonly Utilizador familiar = new("Joana Fictícia", "joana.ficticia@example.com", "hash", Agora);
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificacaoServiceTestes"/> class.
    /// </summary>
    public NotificacaoServiceTestes()
    {
        this.repositorios.Utilizadores.AddRange(new[] { this.principal, this.familiar });
        var relogio = new RelogioFixo(Agora);
        var envioEmail = ServicosDeTeste.EnvioEmailService(this.repositorios, new UnidadeDeTrabalhoEmMemoria(), relogio, new EmailSenderFalso());
        this.servico = new NotificacaoService(this.repositorios, this.repositorios, envioEmail, relogio);
        this.ficha = FichasDeTeste.DeTerceiro(this.principal.Id);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// Os Cuidadores recebem as notificações dirigidas aos Cuidadores e os Familiares as dirigidas aos Familiares; uma associação
    /// a um Utilizador que já não existe é ignorada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DestinatariosSaoOsDoPapelPedido()
    {
        this.ficha.Associar(Guid.NewGuid(), Papel.Familiar, Agora);

        var aosCuidadores = await this.NotificarAsync(familiares: false);
        var aosFamiliares = await this.NotificarAsync(familiares: true);

        Assert.Equal(this.principal.Id, Assert.Single(aosCuidadores).DestinatarioId);
        Assert.Equal(this.familiar.Id, Assert.Single(aosFamiliares).DestinatarioId);
        Assert.Equal(2, this.repositorios.EnviosEmail.Count);
    }

    /// <summary>
    /// RF-142: numa ficha arquivada não é criada nenhuma notificação nem e-mail.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaNaoGeraNotificacoes()
    {
        this.ficha.Arquivar(Agora);

        Assert.Empty(await this.NotificarAsync(familiares: false));
        Assert.Empty(await this.NotificarAsync(familiares: true));
        Assert.Empty(this.repositorios.Notificacoes);
        Assert.Empty(this.repositorios.EnviosEmail);
    }

    /// <summary>
    /// RF-140 CA2 e CA3, RF-119: o histórico tem só as notificações do Utilizador, da mais recente para a mais antiga, com a ficha,
    /// o medicamento e a hora prevista da toma e o estado do e-mail; sem e-mail registado aparece <c>Pendente</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task HistoricoTemSoAsDoUtilizadorDaMaisRecenteParaAMaisAntiga()
    {
        var tratamento = new Tratamento(this.ficha.Id, "Insulina", "10 UI", GrauImportancia.Grave, new DateOnly(2026, 10, 21), null, new[] { new TimeOnly(11, 0) }, this.principal.Id, Agora);
        this.repositorios.Tratamentos.Add(tratamento);
        var toma = new Toma(tratamento.Id, Agora, "Insulina", "10 UI", GrauImportancia.Grave);
        this.repositorios.Tomas.Add(toma);
        var antiga = this.NovaNotificacao(this.familiar.Id, TipoNotificacao.TomaGraveFamiliares, TipoReferencia.Toma, toma.Id, Agora.AddMinutes(61));
        var recente = this.NovaNotificacao(this.familiar.Id, TipoNotificacao.NovaOcorrencia, TipoReferencia.Ocorrencia, Guid.NewGuid(), Agora.AddHours(2));
        this.NovaNotificacao(this.principal.Id, TipoNotificacao.TomaEmAtraso, TipoReferencia.Toma, toma.Id, Agora.AddMinutes(31));
        var envio = new EnvioEmail(antiga.Id, null, this.familiar.Email, "Assunto", antiga.CriadaEmUtc);
        envio.IniciarTentativa(antiga.CriadaEmUtc);
        envio.MarcarEnviado(antiga.CriadaEmUtc);
        this.repositorios.EnviosEmail.Add(envio);

        var historico = await this.servico.HistoricoAsync(this.familiar.Id, null, null, CancellationToken.None);

        Assert.Equal(new[] { recente.Id, antiga.Id }, historico.Select(n => n.Id));
        var deToma = historico[1];
        Assert.Equal("Maria Fictícia Santos", deToma.NomeFicha);
        Assert.Equal("Insulina", deToma.Medicamento);
        Assert.Equal(Agora, deToma.PrevistaEmUtc);
        Assert.Equal(EstadoEnvioEmail.Enviado, deToma.Email);
        Assert.Null(historico[0].Medicamento);
        Assert.Null(historico[0].PrevistaEmUtc);
        Assert.Equal(EstadoEnvioEmail.Pendente, historico[0].Email);
    }

    /// <summary>
    /// OP-51: <c>de</c> e <c>ate</c> filtram pelo dia de Lisboa da criação, com <c>ate</c> inclusivo; <c>ate</c> anterior a
    /// <c>de</c> é recusado com o campo em erro (RNF-028).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task HistoricoFiltraPeloIntervaloDeDias()
    {
        var hoje = DateOnly.FromDateTime(Agora);
        var ontem = this.NovaNotificacao(this.familiar.Id, TipoNotificacao.NovaOcorrencia, TipoReferencia.Ocorrencia, Guid.NewGuid(), Agora.AddDays(-1));
        var deHoje = this.NovaNotificacao(this.familiar.Id, TipoNotificacao.NovaOcorrencia, TipoReferencia.Ocorrencia, Guid.NewGuid(), Agora);

        var soHoje = await this.servico.HistoricoAsync(this.familiar.Id, hoje, hoje, CancellationToken.None);
        var desdeOntem = await this.servico.HistoricoAsync(this.familiar.Id, hoje.AddDays(-1), null, CancellationToken.None);
        var ateOntem = await this.servico.HistoricoAsync(this.familiar.Id, null, hoje.AddDays(-1), CancellationToken.None);
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => this.servico.HistoricoAsync(this.familiar.Id, hoje, hoje.AddDays(-1), CancellationToken.None));

        Assert.Equal(deHoje.Id, Assert.Single(soHoje).Id);
        Assert.Equal(2, desdeOntem.Count);
        Assert.Equal(ontem.Id, Assert.Single(ateOntem).Id);
        Assert.Equal("ate", Assert.Single(erro.Campos).Campo);
    }

    private Task<IReadOnlyList<Notificacao>> NotificarAsync(bool familiares)
    {
        var tipo = familiares ? TipoNotificacao.TomaGraveFamiliares : TipoNotificacao.TomaEmAtraso;
        return familiares
            ? this.servico.NotificarFamiliaresAsync(this.ficha, tipo, TipoReferencia.Toma, Guid.NewGuid(), Agora, "Mensagem.", Agora, CancellationToken.None)
            : this.servico.NotificarCuidadoresAsync(this.ficha, tipo, TipoReferencia.Toma, Guid.NewGuid(), Agora, "Mensagem.", Agora, CancellationToken.None);
    }

    private Notificacao NovaNotificacao(Guid destinatarioId, TipoNotificacao tipo, TipoReferencia referenciaTipo, Guid referenciaId, DateTime criadaEmUtc)
    {
        var notificacao = new Notificacao(destinatarioId, this.ficha.Id, tipo, "Mensagem.", referenciaTipo, referenciaId, criadaEmUtc, criadaEmUtc);
        this.repositorios.Notificacoes.Add(notificacao);
        return notificacao;
    }
}
