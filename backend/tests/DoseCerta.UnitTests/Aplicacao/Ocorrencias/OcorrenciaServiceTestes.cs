// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Application.Ocorrencias;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Ocorrencias;

/// <summary>
/// Testes unitários de <see cref="OcorrenciaService"/> (F009, RF-090 a RF-095; Matriz de Permissões, OP-46 a OP-49, RP-13,
/// RP-15 e RP-18). Em novembro, a hora de Lisboa coincide com UTC.
/// </summary>
public class OcorrenciaServiceTestes
{
    private static readonly DateTime Agora = new(2026, 11, 2, 10, 40, 0, DateTimeKind.Utc);
    private static readonly DateTime AsDezEQuinze = new(2026, 11, 2, 10, 15, 0, DateTimeKind.Utc);
    private static readonly string[] CamposDaOcorrencia = { "descricao", "ocorridaEm" };
    private static readonly string[] DoMaisRecente = { "Dia 2.", "Dia 1." };
    private static readonly string[] AutoresDoMaisRecente = { "Bruno Cuidador", "Ana Principal" };
    private static readonly string[] DesdeODia1 = { "02 00:00", "01 23:59" };
    private static readonly string[] AteAoDia1 = { "01 23:59", "31 23:30" };

    // 31/10 23:30 UTC ainda é dia 31 em Lisboa; 01/11 23:59 UTC é dia 1; 02/11 00:00 UTC já é dia 2.
    private static readonly DateTime[] InstantesNaViragemDoDia =
    {
        new(2026, 10, 31, 23, 30, 0, DateTimeKind.Utc),
        new(2026, 11, 1, 23, 59, 0, DateTimeKind.Utc),
        new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc),
    };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly OcorrenciaService servico;
    private readonly Utilizador principal;
    private readonly Utilizador cuidador;
    private readonly Utilizador familiar;
    private readonly Utilizador estranho;
    private readonly Ficha ficha;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcorrenciaServiceTestes"/> class.
    /// </summary>
    public OcorrenciaServiceTestes()
    {
        this.servico = ServicosDeTeste.OcorrenciaService(this.repositorios, this.unidadeDeTrabalho, this.relogio);
        this.principal = this.Conta("Ana Principal");
        this.cuidador = this.Conta("Bruno Cuidador");
        this.familiar = this.Conta("Carla Familiar");
        this.estranho = this.Conta("Duarte Estranho");

        this.ficha = FichasDeTeste.DeTerceiro(this.principal.Id);
        this.ficha.Associar(this.cuidador.Id, Papel.Cuidador, FichasDeTeste.Agora);
        this.ficha.Associar(this.familiar.Id, Papel.Familiar, FichasDeTeste.Agora);
        this.repositorios.Fichas.Add(this.ficha);

        // O estranho é Cuidador de outra ficha, não desta (RF-093 CA2).
        var outra = FichasDeTeste.DeTerceiro(this.estranho.Id);
        this.repositorios.Fichas.Add(outra);
    }

    /// <summary>
    /// RF-090 CA1 e RF-092 CA2: a ocorrência fica na ficha com o autor, o instante em que aconteceu (10:15) e o do registo (10:40).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarOcorrenciaGuardaOAutorEOsDoisInstantes()
    {
        var registada = await this.servico.RegistarOcorrenciaAsync(
            this.principal.Id,
            this.ficha.Id,
            "  Queda na casa de banho, sem ferimentos visíveis  ",
            AsDezEQuinze.AddMilliseconds(700),
            CancellationToken.None);

        var guardada = Assert.Single(this.repositorios.Ocorrencias);
        Assert.Equal(guardada.Id, registada.Id);
        Assert.Equal(this.ficha.Id, guardada.FichaId);
        Assert.Equal("Queda na casa de banho, sem ferimentos visíveis", guardada.Descricao);
        Assert.Equal(this.principal.Id, guardada.AutorId);
        Assert.Equal(AsDezEQuinze, guardada.OcorreuEmUtc);
        Assert.Equal(Agora, guardada.RegistadaEmUtc);
        Assert.Equal(TipoRegisto.Ocorrencia, registada.Tipo);
        Assert.Equal("Ana Principal", registada.NomeAutor);
        Assert.Equal(AsDezEQuinze, registada.OcorreuEmUtc);
        Assert.Equal(Agora, registada.RegistadaEmUtc);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-091 CA1 e RF-092 CA1: um Cuidador que não é o principal regista uma nota, com ele como autor e o instante do registo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorRegistaNotaComAutorEInstante()
    {
        this.relogio.Agora = new DateTime(2026, 11, 2, 14, 0, 0, DateTimeKind.Utc);

        var registada = await this.servico.RegistarNotaAsync(
            this.cuidador.Id,
            this.ficha.Id,
            "Medicação da tarde entregue pela farmácia",
            CancellationToken.None);

        var guardada = Assert.Single(this.repositorios.Notas);
        Assert.Equal(guardada.Id, registada.Id);
        Assert.Equal("Medicação da tarde entregue pela farmácia", guardada.Texto);
        Assert.Equal(this.cuidador.Id, guardada.AutorId);
        Assert.Equal(this.relogio.Agora, guardada.RegistadaEmUtc);
        Assert.Equal(TipoRegisto.Nota, registada.Tipo);
        Assert.Equal("Bruno Cuidador", registada.NomeAutor);
        Assert.Null(registada.OcorreuEmUtc);
    }

    /// <summary>
    /// RF-115 CA1: a ocorrência do Cuidador principal notifica na aplicação o outro Cuidador e o Familiar, e não o autor; a
    /// notificação refere a ocorrência e tem como instante-limite o instante do registo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task RegistarOcorrenciaNotificaTodosExcetoOAutor()
    {
        var registada = await this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None);

        Assert.Equal(2, this.repositorios.Notificacoes.Count);
        Assert.Equal(
            new[] { this.cuidador.Id, this.familiar.Id }.Order(),
            this.repositorios.Notificacoes.Select(n => n.DestinatarioId).Order());
        Assert.All(this.repositorios.Notificacoes, n =>
        {
            Assert.Equal(this.ficha.Id, n.FichaId);
            Assert.Equal(TipoNotificacao.NovaOcorrencia, n.Tipo);
            Assert.Equal(TipoReferencia.Ocorrencia, n.ReferenciaTipo);
            Assert.Equal(registada.Id, n.ReferenciaId);
            Assert.Equal(Agora, n.InstanteLimiteUtc);
            Assert.Equal(Agora, n.CriadaEmUtc);
            Assert.Equal(
                "Ana Principal registou uma ocorrência na ficha de Maria Fictícia Santos, ocorrida em 02/11/2026 10:15. "
                + "Pode consultá-la nas ocorrências da ficha.",
                n.Mensagem);
        });
    }

    /// <summary>
    /// RF-115 CA1 e CA2: quando o autor é o outro Cuidador, quem recebe é o Cuidador principal e o Familiar; uma nota não
    /// notifica ninguém.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task OAutorNuncaENotificadoEANotaNaoNotifica()
    {
        await this.servico.RegistarNotaAsync(this.cuidador.Id, this.ficha.Id, "Nota.", CancellationToken.None);
        Assert.Empty(this.repositorios.Notificacoes);

        await this.servico.RegistarOcorrenciaAsync(this.cuidador.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None);

        Assert.Equal(
            new[] { this.principal.Id, this.familiar.Id }.Order(),
            this.repositorios.Notificacoes.Select(n => n.DestinatarioId).Order());
    }

    /// <summary>
    /// RNF-046: se a gravação da ocorrência falhar, não fica a ocorrência nem nenhuma notificação.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNaGravacaoNaoDeixaOcorrenciaNemNotificacoes()
    {
        var servicoQueFalha = ServicosDeTeste.OcorrenciaService(this.repositorios, new UnidadeDeTrabalhoQueFalhaAoGravar(this.repositorios), this.relogio);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicoQueFalha.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None));

        Assert.Empty(this.repositorios.Ocorrencias);
        Assert.Empty(this.repositorios.Notificacoes);
    }

    /// <summary>
    /// RF-090 CA2 e RF-091 CA2: sem descrição, sem instante ou com texto demasiado longo, nada é registado e os campos são indicados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DadosEmFaltaOuInvalidosNaoRegistamNada()
    {
        var ocorrencia = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "   ", null, CancellationToken.None));
        var nota = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.servico.RegistarNotaAsync(this.principal.Id, this.ficha.Id, null, CancellationToken.None));
        var longa = await Assert.ThrowsAsync<ValidacaoException>(() => this.servico.RegistarNotaAsync(
            this.principal.Id,
            this.ficha.Id,
            new string('a', OcorrenciaService.ComprimentoMaximoDescricao + 1),
            CancellationToken.None));

        Assert.Equal(CamposDaOcorrencia, ocorrencia.Campos.Select(c => c.Campo));
        Assert.Equal("descricao", Assert.Single(nota.Campos).Campo);
        Assert.Contains("2000", Assert.Single(longa.Campos).Regra, StringComparison.Ordinal);
        Assert.Empty(this.repositorios.Ocorrencias);
        Assert.Empty(this.repositorios.Notas);
        Assert.Equal(0, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-090 CA3, RF-091 CA3 e RNF-005: o Familiar recebe ACESSO_PROIBIDO e quem não está associado recebe FICHA_NAO_ENCONTRADA.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoUmCuidadorDaFichaRegista()
    {
        var ocorrenciaDoFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarOcorrenciaAsync(this.familiar.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None));
        var notaDoFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarNotaAsync(this.familiar.Id, this.ficha.Id, "Nota.", CancellationToken.None));
        var ocorrenciaDoEstranho = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarOcorrenciaAsync(this.estranho.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None));
        var notaDoEstranho = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarNotaAsync(this.estranho.Id, this.ficha.Id, "Nota.", CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, ocorrenciaDoFamiliar.Codigo);
        Assert.Equal(CodigosErro.AcessoProibido, notaDoFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, ocorrenciaDoEstranho.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, notaDoEstranho.Codigo);
        Assert.Empty(this.repositorios.Ocorrencias);
        Assert.Empty(this.repositorios.Notas);
    }

    /// <summary>
    /// RF-120 e RP-13: numa ficha arquivada não se registam ocorrências nem notas, mas as já registadas continuam a ser consultadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaArquivadaSoPermiteConsultar()
    {
        await this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "Queda.", AsDezEQuinze, CancellationToken.None);
        await this.servico.RegistarNotaAsync(this.principal.Id, this.ficha.Id, "Nota.", CancellationToken.None);
        this.ficha.Arquivar(Agora);

        var ocorrencia = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "Outra.", AsDezEQuinze, CancellationToken.None));
        var nota = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.RegistarNotaAsync(this.cuidador.Id, this.ficha.Id, "Outra.", CancellationToken.None));

        Assert.Equal(CodigosErro.FichaArquivada, ocorrencia.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, nota.Codigo);
        Assert.Single(await this.servico.ConsultarOcorrenciasAsync(this.familiar.Id, this.ficha.Id, null, null, CancellationToken.None));
        Assert.Single(await this.servico.ConsultarNotasAsync(this.cuidador.Id, this.ficha.Id, null, null, CancellationToken.None));
    }

    /// <summary>
    /// RF-094 CA1 e CA2: o Familiar vê as ocorrências da mais recente para a mais antiga, com o autor; quem não está associado
    /// recebe FICHA_NAO_ENCONTRADA.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarConsultaAsOcorrenciasDaMaisRecenteParaAMaisAntiga()
    {
        this.relogio.Agora = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
        await this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, "Dia 1.", this.relogio.Agora, CancellationToken.None);
        this.relogio.Agora = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        await this.servico.RegistarOcorrenciaAsync(this.cuidador.Id, this.ficha.Id, "Dia 2.", this.relogio.Agora, CancellationToken.None);

        var lista = await this.servico.ConsultarOcorrenciasAsync(this.familiar.Id, this.ficha.Id, null, null, CancellationToken.None);
        var recusa = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ConsultarOcorrenciasAsync(this.estranho.Id, this.ficha.Id, null, null, CancellationToken.None));

        Assert.Equal(DoMaisRecente, lista.Select(r => r.Descricao));
        Assert.Equal(AutoresDoMaisRecente, lista.Select(r => r.NomeAutor));
        Assert.Equal(CodigosErro.FichaNaoEncontrada, recusa.Codigo);
    }

    /// <summary>
    /// RF-093 CA1 e CA2, RF-095 e RP-15: o outro Cuidador vê a nota na consulta seguinte; o Familiar recebe ACESSO_PROIBIDO e o
    /// Cuidador de outra ficha recebe FICHA_NAO_ENCONTRADA.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NotasSoSaoVisiveisAosCuidadoresDaFicha()
    {
        var nota = await this.servico.RegistarNotaAsync(this.principal.Id, this.ficha.Id, "Reforçar a hidratação.", CancellationToken.None);

        var doCuidador = await this.servico.ConsultarNotasAsync(this.cuidador.Id, this.ficha.Id, null, null, CancellationToken.None);
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ConsultarNotasAsync(this.familiar.Id, this.ficha.Id, null, null, CancellationToken.None));
        var doEstranho = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.ConsultarNotasAsync(this.estranho.Id, this.ficha.Id, null, null, CancellationToken.None));

        Assert.Equal(nota, Assert.Single(doCuidador));
        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, doEstranho.Codigo);
    }

    /// <summary>
    /// OP-48 e OP-49: <c>de</c> e <c>ate</c> são dias de Lisboa aplicados ao instante do registo, com <c>ate</c> inclusivo; o
    /// intervalo invertido é recusado no campo <c>ate</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FiltraPeloDiaDoRegistoEmHoraDeLisboa()
    {
        foreach (var instante in InstantesNaViragemDoDia)
        {
            this.relogio.Agora = instante;
            var texto = instante.ToString("dd HH:mm", CultureInfo.InvariantCulture);
            await this.servico.RegistarOcorrenciaAsync(this.principal.Id, this.ficha.Id, texto, instante, CancellationToken.None);
            await this.servico.RegistarNotaAsync(this.principal.Id, this.ficha.Id, texto, CancellationToken.None);
        }

        var dia = new DateOnly(2026, 11, 1);
        var ocorrencias = await this.servico.ConsultarOcorrenciasAsync(this.familiar.Id, this.ficha.Id, dia, dia, CancellationToken.None);
        var notasDesde = await this.servico.ConsultarNotasAsync(this.cuidador.Id, this.ficha.Id, dia, null, CancellationToken.None);
        var notasAte = await this.servico.ConsultarNotasAsync(this.cuidador.Id, this.ficha.Id, null, dia, CancellationToken.None);
        var invertido = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.servico.ConsultarOcorrenciasAsync(this.familiar.Id, this.ficha.Id, dia, dia.AddDays(-1), CancellationToken.None));

        Assert.Equal("01 23:59", Assert.Single(ocorrencias).Descricao);
        Assert.Equal(DesdeODia1, notasDesde.Select(n => n.Descricao));
        Assert.Equal(AteAoDia1, notasAte.Select(n => n.Descricao));
        Assert.Equal("ate", Assert.Single(invertido.Campos).Campo);
    }

    private Utilizador Conta(string nome)
    {
        var conta = new Utilizador(nome, $"{Guid.NewGuid():N}@exemplo.pt", "hash-ficticio", FichasDeTeste.Agora);
        this.repositorios.Adicionar(conta);
        return conta;
    }
}
