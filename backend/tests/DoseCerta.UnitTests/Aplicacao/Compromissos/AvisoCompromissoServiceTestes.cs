// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Compromissos;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Compromissos;

/// <summary>
/// Testes unitários do passo 6 do processo periódico em <see cref="AvisoCompromissoService"/>: aviso antes de um compromisso
/// (I056: RF-116, RF-117, RF-118, RF-142, RNF-043). A ficha foi criada a 21 de outubro de 2026, 10:00 de Lisboa; o compromisso
/// é a 22 de outubro, 11:30 de Lisboa (10:30 UTC), com 1 hora de antecedência (aviso às 09:30 UTC).
/// </summary>
public class AvisoCompromissoServiceTestes
{
    private static readonly DateTime DataHora = new(2026, 10, 22, 10, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime Aviso = DataHora.AddHours(-1);

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Aviso);
    private readonly NotificacaoService notificacoes;
    private readonly AvisoCompromissoService servico;
    private readonly Utilizador principal = new("Ana Fictícia", "ana.ficticia@example.com", "hash", FichasDeTeste.Agora);
    private readonly Utilizador cuidador = new("Rui Fictício", "rui.ficticio@example.com", "hash", FichasDeTeste.Agora);
    private readonly Utilizador familiar = new("Joana Fictícia", "joana.ficticia@example.com", "hash", FichasDeTeste.Agora);
    private readonly Utilizador estranho = new("Pedro Fictício", "pedro.ficticio@example.com", "hash", FichasDeTeste.Agora);
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvisoCompromissoServiceTestes"/> class.
    /// </summary>
    public AvisoCompromissoServiceTestes()
    {
        this.repositorios.Utilizadores.AddRange(new[] { this.principal, this.cuidador, this.familiar, this.estranho });
        var envioEmail = ServicosDeTeste.EnvioEmailService(this.repositorios, this.unidadeDeTrabalho, this.relogio, new EmailSenderFalso());
        this.notificacoes = new NotificacaoService(this.repositorios, this.repositorios, envioEmail, this.relogio);
        this.servico = this.Servico(this.repositorios);

        this.ficha = FichasDeTeste.DeTerceiro(this.principal.Id);
        this.ficha.Associar(this.cuidador.Id, Papel.Cuidador, FichasDeTeste.Agora);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, FichasDeTeste.Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// RF-117 CA1 e RF-118: no instante do aviso, cada Cuidador (incluindo o principal) e o Familiar recebem uma notificação
    /// <c>ProximoCompromisso</c> com a mensagem em hora de Lisboa e o respetivo e-mail sem dados de saúde no assunto; o aviso
    /// fica marcado e quem não está associado à ficha não recebe nada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NoInstanteDoAvisoNotificaTodosEMarca()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);

        Assert.Equal(1, await this.servico.AvisarProximosAsync(Aviso, CancellationToken.None));

        Assert.Equal(
            new[] { this.principal.Id, this.cuidador.Id, this.familiar.Id }.ToHashSet(),
            this.repositorios.Notificacoes.Select(n => n.DestinatarioId).ToHashSet());
        Assert.All(this.repositorios.Notificacoes, n =>
        {
            Assert.Equal(TipoNotificacao.ProximoCompromisso, n.Tipo);
            Assert.Equal(TipoReferencia.Compromisso, n.ReferenciaTipo);
            Assert.Equal(compromisso.Id, n.ReferenciaId);
            Assert.Equal(Aviso, n.InstanteLimiteUtc);
            Assert.Equal(this.ficha.Id, n.FichaId);
            Assert.Equal(
                "Compromisso de Maria Fictícia Santos: Consulta de rotina, em Centro de Saúde Fictício, a 22/10/2026 às 11:30.",
                n.Mensagem);
        });
        Assert.Equal(3, this.repositorios.EnviosEmail.Count);
        Assert.All(this.repositorios.EnviosEmail, e => Assert.Equal(TextosNotificacao.AssuntoProximoCompromisso, e.Assunto));
        Assert.Equal(Aviso, compromisso.AvisoEnviadoEmUtc);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-117: um segundo antes do instante do aviso não há notificação nem marca.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task AntesDoInstanteNaoCriaNada()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);

        Assert.Equal(0, await this.servico.AvisarProximosAsync(Aviso.AddSeconds(-1), CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
        Assert.Null(compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RNF-043: depois de avisado, os ciclos seguintes não repetem o aviso.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task JaAvisadoNaoRepete()
    {
        this.NovoCompromisso(DataHora, 60);

        await this.servico.AvisarProximosAsync(Aviso, CancellationToken.None);
        Assert.Equal(0, await this.servico.AvisarProximosAsync(Aviso.AddSeconds(30), CancellationToken.None));

        Assert.Equal(3, this.repositorios.Notificacoes.Count);
    }

    /// <summary>
    /// RF-117 CA3: um compromisso registado depois do instante do aviso é avisado no ciclo seguinte.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistadoDepoisDoInstanteEAvisadoNoCicloSeguinte()
    {
        var registo = DataHora.AddMinutes(-10);
        var compromisso = this.NovoCompromisso(DataHora, 1440, registo);

        Assert.Equal(1, await this.servico.AvisarProximosAsync(registo.AddSeconds(30), CancellationToken.None));

        Assert.Equal(3, this.repositorios.Notificacoes.Count);
        Assert.All(this.repositorios.Notificacoes, n => Assert.Equal(compromisso.InstanteAviso(), n.InstanteLimiteUtc));
    }

    /// <summary>
    /// Decisão da I056: um compromisso cuja data já passou (registado depois da data, ou com o servidor parado até lá) nunca
    /// é avisado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DataJaPassadaNuncaEAvisada()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);

        Assert.Equal(0, await this.servico.AvisarProximosAsync(DataHora, CancellationToken.None));
        Assert.Equal(0, await this.servico.AvisarProximosAsync(DataHora.AddDays(1), CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
        Assert.Null(compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RF-122: a ficha arquivada não gera avisos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaNaoCriaNada()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);
        this.ficha.Arquivar(Aviso.AddHours(-2));

        Assert.Equal(0, await this.servico.AvisarProximosAsync(Aviso, CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
        Assert.Null(compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RF-142: a ficha reativada depois do instante do aviso não recebe o aviso do período em que esteve arquivada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaReativadaDepoisDoInstanteNaoCriaNada()
    {
        this.NovoCompromisso(DataHora, 60);
        this.ficha.Arquivar(Aviso.AddHours(-2));
        this.ficha.Reativar(Aviso.AddMinutes(10));

        Assert.Equal(0, await this.servico.AvisarProximosAsync(Aviso.AddMinutes(20), CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RF-142: a verificação repete-se dentro da transação; uma ficha arquivada depois da procura não é avisada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaDepoisDaProcuraNaoCriaNada()
    {
        this.NovoCompromisso(DataHora, 60);
        var servicoComDemora = this.Servico(new RepositorioComAcaoDepoisDeListar(this.repositorios, () => this.ficha.Arquivar(Aviso)));

        Assert.Equal(0, await servicoComDemora.AvisarProximosAsync(Aviso, CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RF-117 CA2: o compromisso removido entre a procura e a transação não é avisado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RemovidoDepoisDaProcuraNaoCriaNada()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);
        var servicoComRemocao = this.Servico(new RepositorioComAcaoDepoisDeListar(
            this.repositorios, () => this.repositorios.Compromissos.Remove(compromisso)));

        Assert.Equal(0, await servicoComRemocao.AvisarProximosAsync(Aviso, CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RNF-043: se outra execução marcou o aviso (ou uma edição mudou a data) entre a leitura e a marca, não há notificações.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MarcaRecusadaNaoCriaNotificacoes()
    {
        this.NovoCompromisso(DataHora, 60);
        var servicoConcorrente = this.Servico(new RepositorioComAcaoDepoisDeListar(this.repositorios, () => { }, marcaAceite: false));

        Assert.Equal(0, await servicoConcorrente.AvisarProximosAsync(Aviso, CancellationToken.None));

        Assert.Empty(this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RF-081 e RF-117: editar a data repõe o aviso, que volta a sair no instante novo com a data nova.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditadoVoltaAAvisarNoInstanteNovo()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);
        await this.servico.AvisarProximosAsync(Aviso, CancellationToken.None);
        compromisso.Atualizar(DataHora.AddDays(1), compromisso.Local, compromisso.Motivo, 60);

        Assert.Equal(0, await this.servico.AvisarProximosAsync(Aviso.AddMinutes(1), CancellationToken.None));
        Assert.Equal(1, await this.servico.AvisarProximosAsync(Aviso.AddDays(1), CancellationToken.None));

        var novas = this.repositorios.Notificacoes.Where(n => n.InstanteLimiteUtc == Aviso.AddDays(1)).ToList();
        Assert.Equal(3, novas.Count);
        Assert.All(novas, n => Assert.EndsWith("a 23/10/2026 às 11:30.", n.Mensagem, StringComparison.Ordinal));
    }

    /// <summary>
    /// RNF-043: uma edição que mantém o instante do aviso (mais 1 hora na data e na antecedência) volta a marcar o aviso, mas
    /// não repete a notificação a quem já a recebeu para esse instante (<c>UQ_Notificacao_Origem</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditadoComOMesmoInstanteNaoRepeteANotificacao()
    {
        var compromisso = this.NovoCompromisso(DataHora, 60);
        await this.servico.AvisarProximosAsync(Aviso, CancellationToken.None);
        compromisso.Atualizar(DataHora.AddHours(1), compromisso.Local, compromisso.Motivo, 120);

        Assert.Equal(1, await this.servico.AvisarProximosAsync(Aviso.AddMinutes(1), CancellationToken.None));

        Assert.Equal(3, this.repositorios.Notificacoes.Count);
        Assert.Equal(Aviso.AddMinutes(1), compromisso.AvisoEnviadoEmUtc);
    }

    /// <summary>
    /// RNF-029: a data e a hora da mensagem seguem a hora de Lisboa também depois da mudança para a hora de inverno
    /// (25 de outubro de 2026): 10:30 UTC é 11:30 a 22 de outubro e 10:30 a 26 de outubro.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task MensagemEmHoraDeLisboaDepoisDaMudancaDeHora()
    {
        var depoisDaMudanca = new DateTime(2026, 10, 26, 10, 30, 0, DateTimeKind.Utc);
        this.NovoCompromisso(depoisDaMudanca, 2880);

        Assert.Equal(1, await this.servico.AvisarProximosAsync(depoisDaMudanca.AddDays(-2), CancellationToken.None));

        Assert.All(this.repositorios.Notificacoes, n => Assert.EndsWith("a 26/10/2026 às 10:30.", n.Mensagem, StringComparison.Ordinal));
    }

    private AvisoCompromissoService Servico(ICompromissoRepositorio compromissos)
    {
        return new AvisoCompromissoService(compromissos, this.repositorios, this.notificacoes, this.unidadeDeTrabalho, this.relogio);
    }

    private Compromisso NovoCompromisso(DateTime dataHoraUtc, int antecedencia, DateTime? criadoEmUtc = null)
    {
        var compromisso = new Compromisso(
            this.ficha.Id,
            dataHoraUtc,
            "Centro de Saúde Fictício",
            "Consulta de rotina",
            antecedencia,
            this.principal.Id,
            criadoEmUtc ?? FichasDeTeste.Agora);
        this.repositorios.Compromissos.Add(compromisso);
        return compromisso;
    }

    /// <summary>
    /// Repositório que executa uma ação depois de listar os compromissos por avisar, para simular alterações feitas por outro
    /// pedido entre a procura e a transação, e que pode recusar a marca condicional.
    /// </summary>
    private sealed class RepositorioComAcaoDepoisDeListar : ICompromissoRepositorio
    {
        private readonly ICompromissoRepositorio interno;
        private readonly Action depoisDeListar;
        private readonly bool marcaAceite;

        public RepositorioComAcaoDepoisDeListar(ICompromissoRepositorio interno, Action depoisDeListar, bool marcaAceite = true)
        {
            this.interno = interno;
            this.depoisDeListar = depoisDeListar;
            this.marcaAceite = marcaAceite;
        }

        public async Task<IReadOnlyList<Guid>> PorAvisarAsync(DateTime agoraUtc, CancellationToken cancelamento)
        {
            var lista = await this.interno.PorAvisarAsync(agoraUtc, cancelamento);
            this.depoisDeListar();
            return lista;
        }

        public Task<bool> MarcarAvisoEnviadoAsync(Compromisso lido, DateTime agoraUtc, CancellationToken cancelamento)
        {
            return this.marcaAceite ? this.interno.MarcarAvisoEnviadoAsync(lido, agoraUtc, cancelamento) : Task.FromResult(false);
        }

        public Task<Compromisso?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
        {
            return this.interno.ObterPorIdAsync(id, cancelamento);
        }

        public Task<IReadOnlyList<Compromisso>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento)
        {
            return this.interno.DaFichaAsync(fichaId, deUtc, ateUtc, cancelamento);
        }

        public Task<IReadOnlyList<Compromisso>> DoPeriodoDasFichasAsync(
            IReadOnlyCollection<Guid> fichaIds,
            DateTime deUtc,
            DateTime ateUtc,
            CancellationToken cancelamento)
        {
            return this.interno.DoPeriodoDasFichasAsync(fichaIds, deUtc, ateUtc, cancelamento);
        }

        public void GravarAvisoReposto(Compromisso compromisso)
        {
            this.interno.GravarAvisoReposto(compromisso);
        }

        public void Adicionar(Compromisso compromisso)
        {
            this.interno.Adicionar(compromisso);
        }

        public void Remover(Compromisso compromisso)
        {
            this.interno.Remover(compromisso);
        }
    }
}
