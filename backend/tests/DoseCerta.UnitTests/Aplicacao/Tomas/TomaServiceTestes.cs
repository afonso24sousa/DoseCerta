// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Tomas;

/// <summary>
/// Testes unitários das operações da F006 em <see cref="TomaService"/>: listagem, confirmação, calendário e passos do
/// processo periódico (RF-060 a RF-069, RNF-044; TR-03 a TR-15). O relógio está fixo em 7 de outubro de 2026, 08:30 de Lisboa.
/// </summary>
public class TomaServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 7, 7, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime OitoDeLisboa = new(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc);
    private static readonly TimeOnly[] HorasDoDia = { new(8, 0), new(20, 0) };
    private static readonly string[] CamposDoIntervalo = { "de", "ate" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly TomaService servico;
    private readonly Utilizador ana = new("Ana Fictícia", "ana.ficticia@example.com", "hash", Agora.AddDays(-60));
    private readonly Utilizador rui = new("Rui Fictício", "rui.ficticio@example.com", "hash", Agora.AddDays(-60));
    private readonly Utilizador familiar = new("Joana Fictícia", "joana.ficticia@example.com", "hash", Agora.AddDays(-60));
    private readonly Ficha ficha;
    private readonly Tratamento tratamento;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomaServiceTestes"/> class.
    /// </summary>
    public TomaServiceTestes()
    {
        this.repositorios.Utilizadores.AddRange(new[] { this.ana, this.rui, this.familiar });
        this.servico = new TomaService(
            this.repositorios,
            this.repositorios,
            this.repositorios,
            this.repositorios,
            new AutorizacaoFichaService(this.repositorios),
            this.unidadeDeTrabalho,
            this.relogio);

        this.ficha = this.NovaFicha("Maria Fictícia Santos", this.ana.Id);
        this.ficha.Associar(this.rui.Id, Papel.Cuidador, Agora.AddDays(-30));
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, Agora.AddDays(-30));
        this.tratamento = this.NovoTratamento(this.ficha);
    }

    /// <summary>
    /// TR-03 e RF-064: o Cuidador confirma dentro da tolerância; ficam o autor, o nome e o instante.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorConfirmaDentroDaTolerancia()
    {
        var toma = this.NovaToma(OitoDeLisboa);

        var confirmada = await this.servico.ConfirmarAsync(this.rui.Id, toma.Id, CancellationToken.None);

        Assert.Same(toma, confirmada.Toma);
        Assert.Equal("Rui Fictício", confirmada.ConfirmadaPorNome);
        Assert.Equal(EstadoToma.Confirmada, toma.Estado);
        Assert.Equal(this.rui.Id, toma.ConfirmadaPorId);
        Assert.Equal(Agora, toma.ConfirmadaEmUtc);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// TR-12: o Familiar não confirma; a toma fica pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoConfirma()
    {
        var toma = this.NovaToma(OitoDeLisboa);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ConfirmarAsync(this.familiar.Id, toma.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, erro.Codigo);
        Assert.Equal(EstadoToma.Pendente, toma.Estado);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RNF-005: a toma inexistente e a de uma ficha não associada dão o mesmo <c>RECURSO_NAO_ENCONTRADO</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaInexistenteOuDeOutraFichaNaoEncontrada()
    {
        var toma = this.NovaToma(OitoDeLisboa);

        var inexistente = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.ConfirmarAsync(this.rui.Id, Guid.NewGuid(), CancellationToken.None));
        var naoAssociado = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.ConfirmarAsync(Guid.NewGuid(), toma.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.RecursoNaoEncontrado, inexistente.Codigo);
        Assert.Equal(CodigosErro.RecursoNaoEncontrado, naoAssociado.Codigo);
        Assert.Equal(inexistente.Message, naoAssociado.Message);
    }

    /// <summary>
    /// TR-15 e RF-105: a segunda confirmação é recusada com quem confirmou e quando; a primeira mantém-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SegundaConfirmacaoDevolveAConfirmacaoGravada()
    {
        var toma = this.NovaToma(OitoDeLisboa);
        this.relogio.Agora = OitoDeLisboa.AddMinutes(5);
        await this.servico.ConfirmarAsync(this.ana.Id, toma.Id, CancellationToken.None);
        this.relogio.Agora = OitoDeLisboa.AddMinutes(20);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ConfirmarAsync(this.rui.Id, toma.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.TomaJaConfirmada, erro.Codigo);
        Assert.Equal("A toma já foi confirmada por Ana Fictícia em 07/10/2026 às 08:05.", erro.Message);
        Assert.Equal(new ConfirmacaoRegistada(this.ana.Id, "Ana Fictícia", OitoDeLisboa.AddMinutes(5)), erro.Confirmacao);
        Assert.Equal(this.ana.Id, toma.ConfirmadaPorId);
    }

    /// <summary>
    /// RNF-044: se outro Cuidador gravar primeiro (conflito de versão), a resposta é <c>TOMA_JA_CONFIRMADA</c> com a confirmação gravada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConflitoDeVersaoDevolveTomaJaConfirmada()
    {
        var toma = this.NovaToma(OitoDeLisboa);
        this.unidadeDeTrabalho.AntesDeConfirmar = () => throw new ConflitoDeVersaoException();

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ConfirmarAsync(this.rui.Id, toma.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.TomaJaConfirmada, erro.Codigo);
        Assert.NotNull(erro.Confirmacao);
        Assert.Equal(this.rui.Id, erro.Confirmacao.ConfirmadaPorId);
    }

    /// <summary>
    /// RNF-044: um conflito de versão sem confirmação gravada não é escondido.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConflitoDeVersaoSemConfirmacaoVoltaASerLancado()
    {
        var toma = this.NovaToma(OitoDeLisboa);
        this.unidadeDeTrabalho.AntesDeConfirmar = () =>
        {
            this.repositorios.Tomas.Remove(toma);
            throw new ConflitoDeVersaoException();
        };

        await Assert.ThrowsAsync<ConflitoDeVersaoException>(() => this.servico.ConfirmarAsync(this.rui.Id, toma.Id, CancellationToken.None));
    }

    /// <summary>
    /// RP-13: numa ficha arquivada, as tomas ainda se confirmam (não dá <c>FICHA_ARQUIVADA</c>).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaAindaConfirma()
    {
        var toma = this.NovaToma(OitoDeLisboa);
        this.ficha.Arquivar(Agora);

        var confirmada = await this.servico.ConfirmarAsync(this.ana.Id, toma.Id, CancellationToken.None);

        Assert.Equal(EstadoToma.Confirmada, confirmada.Toma.Estado);
    }

    /// <summary>
    /// TR-13: a toma de amanhã ainda não pode ser confirmada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TomaDeAmanhaFicaForaDaJanela()
    {
        var toma = this.NovaToma(OitoDeLisboa.AddDays(1));

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ConfirmarAsync(this.ana.Id, toma.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.TomaForaDaJanela, erro.Codigo);
    }

    /// <summary>
    /// RF-063: Cuidadores e Familiares listam as tomas dos dias de Lisboa pedidos, por ordem; o filtro pelo estado aplica-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarTomasDoDiaDeLisboa()
    {
        var vespera = this.NovaToma(new DateTime(2026, 10, 6, 22, 30, 0, DateTimeKind.Utc));
        var madrugada = this.NovaToma(new DateTime(2026, 10, 6, 23, 30, 0, DateTimeKind.Utc));
        var manha = this.NovaToma(OitoDeLisboa);
        await this.servico.ConfirmarAsync(this.ana.Id, manha.Id, CancellationToken.None);
        var dia = new DateOnly(2026, 10, 7);

        var doFamiliar = await this.servico.ListarAsync(this.familiar.Id, this.ficha.Id, dia, dia, null, null, CancellationToken.None);
        var confirmadas = await this.servico.ListarAsync(this.ana.Id, this.ficha.Id, dia, dia, EstadoToma.Confirmada, this.tratamento.Id, CancellationToken.None);

        Assert.Equal(new[] { madrugada.Id, manha.Id }, doFamiliar.Select(t => t.Toma.Id));
        Assert.DoesNotContain(doFamiliar, t => t.Toma.Id == vespera.Id);
        var confirmada = Assert.Single(confirmadas);
        Assert.Equal("Ana Fictícia", confirmada.ConfirmadaPorNome);
    }

    /// <summary>
    /// OP-32: o intervalo é obrigatório e tem no máximo 31 dias, contando o primeiro e o último.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarValidaOIntervalo()
    {
        var outubro = new DateOnly(2026, 10, 1);

        var semDatas = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ListarAsync(this.ana.Id, this.ficha.Id, null, null, null, null, CancellationToken.None));
        var invertido = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ListarAsync(this.ana.Id, this.ficha.Id, outubro, outubro.AddDays(-1), null, null, CancellationToken.None));
        var longo = await Assert.ThrowsAsync<ValidacaoException>(() =>
            this.servico.ListarAsync(this.ana.Id, this.ficha.Id, outubro, outubro.AddDays(31), null, null, CancellationToken.None));
        var mesInteiro = await this.servico.ListarAsync(this.ana.Id, this.ficha.Id, outubro, outubro.AddDays(30), null, null, CancellationToken.None);

        Assert.Equal(CamposDoIntervalo, semDatas.Campos.Select(c => c.Campo));
        Assert.Equal("ate", Assert.Single(invertido.Campos).Campo);
        Assert.Equal("ate", Assert.Single(longo.Campos).Campo);
        Assert.Empty(mesInteiro);
    }

    /// <summary>
    /// RNF-005: quem não está associado recebe <c>FICHA_NAO_ENCONTRADA</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarSemAssociacaoNaoEncontraAFicha()
    {
        var dia = new DateOnly(2026, 10, 7);

        var erro = await Assert.ThrowsAsync<DominioException>(() =>
            this.servico.ListarAsync(Guid.NewGuid(), this.ficha.Id, dia, dia, null, null, CancellationToken.None));

        Assert.Equal(CodigosErro.FichaNaoEncontrada, erro.Codigo);
    }

    /// <summary>
    /// RF-061 e RF-062: o calendário tem só as tomas pendentes das próximas 48 horas das fichas ativas em que o Utilizador é Cuidador.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CalendarioTemSoAsPendentesDasProximas48Horas()
    {
        var daquiAUmaHora = this.NovaToma(Agora.AddHours(1));
        var noLimite = this.NovaToma(Agora.AddHours(48).AddSeconds(-1));
        this.NovaToma(Agora.AddHours(48));
        this.NovaToma(Agora.AddMinutes(-1));
        var confirmada = this.NovaToma(Agora.AddHours(2));
        await this.servico.ConfirmarAsync(this.ana.Id, confirmada.Id, CancellationToken.None);

        var outra = this.NovaFicha("Carlos Fictício", Guid.NewGuid());
        outra.Associar(this.ana.Id, Papel.Familiar, Agora.AddDays(-30));
        this.NovaToma(Agora.AddHours(3), this.NovoTratamento(outra));
        var arquivada = this.NovaFicha("Rosa Fictícia", this.ana.Id);
        this.NovaToma(Agora.AddHours(4), this.NovoTratamento(arquivada));
        arquivada.Arquivar(Agora);

        var calendario = await this.servico.CalendarioParaSincronizarAsync(this.ana.Id, CancellationToken.None);
        var doFamiliar = await this.servico.CalendarioParaSincronizarAsync(this.familiar.Id, CancellationToken.None);

        Assert.Equal(new[] { daquiAUmaHora.Id, noLimite.Id }, calendario.Select(t => t.TomaId));
        Assert.All(calendario, t => Assert.Equal("Maria Fictícia Santos", t.NomeFicha));
        Assert.Empty(doFamiliar);
    }

    /// <summary>
    /// Passo 5 do processo periódico (TR-06): só as pendentes com 24 horas passam a não confirmadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConcluirNaoConfirmadasSoComVinteEQuatroHoras()
    {
        var antiga = this.NovaToma(Agora.AddHours(-24));
        var recente = this.NovaToma(Agora.AddHours(-23));

        var concluidas = await this.servico.ConcluirNaoConfirmadasAsync(Agora, CancellationToken.None);

        Assert.Equal(1, concluidas);
        Assert.Equal(EstadoToma.NaoConfirmada, antiga.Estado);
        Assert.Equal(Agora, antiga.NaoConfirmadaDetetadaEmUtc);
        Assert.Equal(EstadoToma.Pendente, recente.Estado);
    }

    /// <summary>
    /// Passo 1 do processo periódico (RF-060): a janela móvel avança e geram-se só as tomas em falta, sem duplicados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ProlongarJanelaMovelGeraAsTomasEmFalta()
    {
        await this.servico.GerarParaTratamentoAsync(this.tratamento, Agora, CancellationToken.None);
        var antes = this.repositorios.Tomas.Count;
        var depois = Agora.AddDays(5);

        var geradas = await this.servico.PrologarJanelaMovelAsync(depois, CancellationToken.None);
        var repetidas = await this.servico.PrologarJanelaMovelAsync(depois, CancellationToken.None);

        Assert.Equal(10, geradas);
        Assert.Equal(0, repetidas);
        Assert.Equal(antes + 10, this.repositorios.Tomas.Count);
        Assert.True(this.repositorios.Tomas.Max(t => t.PrevistaEmUtc) < depois.AddDays(30));
    }

    /// <summary>
    /// RF-069: o passo 1 não gera tomas para fichas arquivadas nem para tratamentos terminados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ProlongarJanelaMovelIgnoraFichasArquivadasETratamentosTerminados()
    {
        this.ficha.Arquivar(Agora);
        var outra = this.NovaFicha("Carlos Fictício", this.ana.Id);
        this.NovoTratamento(outra).Terminar(Agora);

        var geradas = await this.servico.PrologarJanelaMovelAsync(Agora, CancellationToken.None);

        Assert.Equal(0, geradas);
        Assert.Empty(this.repositorios.Tomas);
    }

    /// <summary>
    /// TR-10: ao retomar a geração de uma ficha reativada, só há tomas a partir do instante da reativação.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RetomarGeracaoSoAPartirDaReativacao()
    {
        this.ficha.Arquivar(Agora.AddDays(-10));
        this.ficha.Reativar(Agora);

        var geradas = await this.servico.RetomarGeracaoDaFichaAsync(this.ficha.Id, Agora, CancellationToken.None);

        // De 7 de outubro, 08:30 de Lisboa, a 6 de novembro, 07:30 UTC: 20:00 do dia 7, 08:00 e 20:00 de 8 de outubro a 5 de novembro.
        Assert.Equal(59, geradas);
        Assert.All(this.repositorios.Tomas, t => Assert.True(t.PrevistaEmUtc >= Agora));
    }

    private Ficha NovaFicha(string nome, Guid principalId)
    {
        var nova = new Ficha(nome, new DateOnly(1941, 5, 17), "+351 910 000 001", null, principalId, Agora.AddDays(-30));
        nova.AssociarCuidadorPrincipal(principalId, provisorio: false, Agora.AddDays(-30));
        this.repositorios.Fichas.Add(nova);
        return nova;
    }

    private Tratamento NovoTratamento(Ficha daFicha)
    {
        var novo = new Tratamento(
            daFicha.Id, "Metformina", "500 mg", GrauImportancia.Moderado, new DateOnly(2026, 10, 1), null, HorasDoDia, this.ana.Id, Agora.AddDays(-6));
        this.repositorios.Tratamentos.Add(novo);
        return novo;
    }

    private Toma NovaToma(DateTime previstaUtc, Tratamento? doTratamento = null)
    {
        var toma = new Toma((doTratamento ?? this.tratamento).Id, previstaUtc, "Metformina", "500 mg", GrauImportancia.Moderado);
        this.repositorios.Tomas.Add(toma);
        return toma;
    }
}
