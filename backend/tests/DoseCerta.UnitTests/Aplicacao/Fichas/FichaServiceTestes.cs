// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Fichas;

/// <summary>
/// Testes unitários das regras de negócio de <see cref="FichaService"/> (F002, F003).
/// </summary>
public class FichaServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DefinicaoPrincipal Eu = new(true, null);
    private static readonly string[] CamposObrigatorios = { "nomeCompleto", "dataNascimento", "contactoEmergencia", "cuidadorPrincipal" };
    private static readonly string[] CamposDataEContacto = { "dataNascimento", "contactoEmergencia" };
    private static readonly string[] CamposDasListas = { "grauAutonomiaId", "condicoesSaudeIds", "alergiasIds" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly ArmazenamentoEmMemoria armazenamento = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly RelogioFixo relogio = new(Agora);
    private readonly FichaService servico;
    private readonly Guid criador = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="FichaServiceTestes"/> class.
    /// </summary>
    public FichaServiceTestes()
    {
        var autorizacao = new AutorizacaoFichaService(this.repositorios);
        var documentos = new DocumentoService(this.repositorios, this.armazenamento, autorizacao, this.unidadeDeTrabalho, this.relogio);
        this.servico = new FichaService(
            this.repositorios,
            this.repositorios,
            new ConviteService(this.repositorios, this.repositorios, this.repositorios, autorizacao, this.unidadeDeTrabalho, this.relogio),
            this.repositorios,
            this.repositorios,
            documentos,
            autorizacao,
            this.unidadeDeTrabalho,
            this.relogio);
    }

    /// <summary>
    /// RF-020 CA1 e RF-024 CA1: a ficha de terceiro fica ativa, com as necessidades e o criador como Cuidador principal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaDeTerceiroComOCriadorComoPrincipal()
    {
        var acesso = await this.CriarAsync(Dados(grau: 2, condicoes: new short[] { 1, 1 }, alergias: new short[] { 2 }));

        var ficha = Assert.Single(this.repositorios.Fichas);
        Assert.Same(ficha, acesso.Ficha);
        Assert.Equal(EstadoFicha.Ativa, ficha.Estado);
        Assert.Equal(TipoFicha.Terceira, ficha.Tipo);
        Assert.Equal("Maria Fictícia Santos", ficha.NomeCompleto);
        Assert.Equal((short)2, ficha.GrauAutonomiaId);
        Assert.Equal((short)1, Assert.Single(ficha.CondicoesSaude).Id);
        Assert.Equal((short)2, Assert.Single(ficha.Alergias).Id);
        Assert.Equal(this.criador, acesso.Associacao.UtilizadorId);
        Assert.True(acesso.Associacao.EPrincipal);
        Assert.False(acesso.Associacao.EProvisorio);
        Assert.Empty(this.repositorios.Convites);
        Assert.Equal(1, this.unidadeDeTrabalho.Confirmadas);
    }

    /// <summary>
    /// RF-025 CA1: com outra pessoa para Cuidador principal, o criador fica provisório e o convite fica pendente.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarComConviteDeixaOCriadorProvisorio()
    {
        var acesso = await this.CriarAsync(Dados(), principal: new DefinicaoPrincipal(false, " rui@exemplo.pt "));

        Assert.True(acesso.Associacao.EPrincipal);
        Assert.True(acesso.Associacao.EProvisorio);
        var convite = Assert.Single(this.repositorios.Convites);
        Assert.Equal("rui@exemplo.pt", convite.EmailConvidado);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.Equal(Papel.Cuidador, convite.PapelOferecido);
        Assert.True(convite.ParaPrincipal);
        Assert.Equal(acesso.Ficha.Id, convite.FichaId);
        Assert.Equal(this.criador, convite.ConvidadoPorId);
    }

    /// <summary>
    /// RF-030: a ficha própria tem o criador como titular e Cuidador principal, sem precisar de cuidadorPrincipal.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaPropria()
    {
        var acesso = await this.CriarAsync(Dados(), propria: true, principal: null);

        Assert.True(acesso.Ficha.EProprio());
        Assert.Equal(this.criador, acesso.Ficha.UtilizadorProprioId);
        Assert.True(acesso.Associacao.EPrincipal);
        Assert.False(acesso.Associacao.EProvisorio);
    }

    /// <summary>
    /// RF-031: a segunda ficha própria é recusada; uma ficha de terceiro continua a ser permitida.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SoUmaFichaPropriaPorUtilizador()
    {
        await this.CriarAsync(Dados(), propria: true, principal: null);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.CriarAsync(Dados(), propria: true, principal: null));
        await this.CriarAsync(Dados());

        Assert.Equal(CodigosErro.FichaPropriaJaExiste, erro.Codigo);
        Assert.Equal(2, this.repositorios.Fichas.Count);
    }

    /// <summary>
    /// RF-020 CA2 e RNF-028: os campos obrigatórios em falta e a data de nascimento futura são indicados, e a ficha não é criada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DadosObrigatoriosEmFaltaSaoIndicados()
    {
        var amanha = DateOnly.FromDateTime(Agora).AddDays(1);

        var semDados = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarAsync(new DadosFicha(" ", null, null, null, null, null), principal: new DefinicaoPrincipal(null, null)));
        var futura = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarAsync(Dados() with { DataNascimento = amanha, ContactoEmergencia = "abc" }));

        Assert.Equal(CamposObrigatorios, semDados.Campos.Select(c => c.Campo).ToArray());
        Assert.Equal(CamposDataEContacto, futura.Campos.Select(c => c.Campo).ToArray());
        Assert.Empty(this.repositorios.Fichas);
    }

    /// <summary>
    /// A data de nascimento anterior a 1900 é recusada e a de hoje é aceite.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task LimitesDaDataDeNascimento()
    {
        var antiga = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarAsync(Dados() with { DataNascimento = new DateOnly(1899, 12, 31) }));
        var hoje = await this.CriarAsync(Dados() with { DataNascimento = DateOnly.FromDateTime(Agora) });

        Assert.Equal("dataNascimento", Assert.Single(antiga.Campos).Campo);
        Assert.Equal(DateOnly.FromDateTime(Agora), hoje.Ficha.DataNascimento);
    }

    /// <summary>
    /// RF-021 CA2: valores fora das listas fechadas são recusados.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ValoresForaDasListasFechadasSaoRecusados()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarAsync(Dados(grau: 99, condicoes: new short[] { 1, 99 }, alergias: new short[] { 98 })));

        Assert.Equal(CamposDasListas, erro.Campos.Select(c => c.Campo).ToArray());
    }

    /// <summary>
    /// RF-024 CA2: a ficha de terceiro sem Cuidador principal, ou com convite sem e-mail válido, não é criada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaDeTerceiroExigeCuidadorPrincipal()
    {
        var semPrincipal = await Assert.ThrowsAsync<ValidacaoException>(() => this.CriarAsync(Dados(), principal: new DefinicaoPrincipal(null, null)));
        var emailInvalido = await Assert.ThrowsAsync<ValidacaoException>(() => this.CriarAsync(Dados(), principal: new DefinicaoPrincipal(false, "x")));

        Assert.Equal("cuidadorPrincipal", Assert.Single(semPrincipal.Campos).Campo);
        Assert.Equal("cuidadorPrincipal.email", Assert.Single(emailInvalido.Campos).Campo);
        Assert.Empty(this.repositorios.Fichas);
    }

    /// <summary>
    /// RF-022: os documentos enviados na criação ficam associados à ficha com a categoria indicada.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarComDocumentosGuardaOsFicheiros()
    {
        var acesso = await this.CriarAsync(
            Dados(),
            documentos: new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf), FicheirosTeste.Documento(FicheirosTeste.Jpg, "DocumentoIdentificacao", "cc.jpg") });

        Assert.Equal(2, this.repositorios.Documentos.Count);
        Assert.All(this.repositorios.Documentos, d => Assert.Equal(acesso.Ficha.Id, d.FichaId));
        Assert.Contains(this.repositorios.Documentos, d => d.Categoria == CategoriaDocumento.DocumentoIdentificacao && d.Formato == FormatoDocumento.JPG);
        Assert.Equal(2, this.armazenamento.Ficheiros.Count);
    }

    /// <summary>
    /// RF-023 CA1: se um documento não puder ser guardado, a ficha não é criada e nenhum ficheiro fica no armazenamento.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNumDocumentoNaoCriaAFicha()
    {
        var unidadeReal = new UnidadeDeTrabalhoComReversao(this.repositorios);
        var servicoComRegisto = this.ServicoCom(unidadeReal);
        this.armazenamento.GravacoesAntesDeFalhar = 1;

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => servicoComRegisto.CriarAsync(
                this.criador,
                Dados(),
                false,
                Eu,
                new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf), FicheirosTeste.Documento(FicheirosTeste.Png, nome: "foto.png") },
                CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoNaoGuardado, erro.Codigo);
        Assert.Empty(this.repositorios.Fichas);
        Assert.Empty(this.repositorios.Documentos);
        Assert.Empty(this.armazenamento.Ficheiros);
    }

    /// <summary>
    /// Um documento inválido é recusado antes de qualquer ficheiro ser guardado, com o índice do documento no campo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DocumentoInvalidoNaCriacaoIndicaOIndice()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarAsync(Dados(), documentos: new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf), FicheirosTeste.Documento(FicheirosTeste.Pdf, "Outra") }));

        Assert.Equal("documentos[1].categoria", Assert.Single(erro.Campos).Campo);
        Assert.Empty(this.armazenamento.Ficheiros);
        Assert.Empty(this.repositorios.Fichas);
    }

    /// <summary>
    /// OP-07: a lista só tem as fichas do Utilizador, filtradas pelo estado quando pedido (RNF-005).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ListarDevolveSoAsFichasDoUtilizador()
    {
        var ativa = await this.CriarAsync(Dados());
        var arquivada = await this.CriarAsync(Dados() with { NomeCompleto = "Ana Fictícia" });
        arquivada.Ficha.Arquivar(Agora);
        var outro = Guid.NewGuid();
        await this.servico.CriarAsync(outro, Dados(), false, Eu, Array.Empty<DocumentoSubmetido>(), CancellationToken.None);

        var todas = await this.servico.ListarAsync(this.criador, null, CancellationToken.None);
        var ativas = await this.servico.ListarAsync(this.criador, EstadoFicha.Ativa, CancellationToken.None);

        Assert.Equal(new[] { arquivada.Ficha.Id, ativa.Ficha.Id }, todas.Select(a => a.Ficha.Id).ToArray());
        Assert.Equal(ativa.Ficha.Id, Assert.Single(ativas).Ficha.Id);
        Assert.All(todas, a => Assert.Equal(this.criador, a.Associacao.UtilizadorId));
    }

    /// <summary>
    /// OP-10: o Familiar consulta a ficha e quem não está associado recebe 404 (RF-028).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ObterRespeitaAAssociacao()
    {
        var criada = await this.CriarAsync(Dados());
        var familiar = Guid.NewGuid();
        criada.Ficha.Associar(familiar, Papel.Familiar, Agora);

        var doFamiliar = await this.servico.ObterAsync(familiar, criada.Ficha.Id, CancellationToken.None);
        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(Guid.NewGuid(), criada.Ficha.Id, CancellationToken.None));

        Assert.Equal(Papel.Familiar, doFamiliar.Associacao.Papel);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, erro.Codigo);
    }

    /// <summary>
    /// RF-029 CA1: o Cuidador altera os dados e as necessidades e acrescenta documentos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarGuardaAsAlteracoes()
    {
        var criada = await this.CriarAsync(Dados(grau: 2, condicoes: new short[] { 1 }, alergias: new short[] { 1 }));

        var editada = await this.servico.EditarAsync(
            this.criador,
            criada.Ficha.Id,
            new DadosFicha("Maria Santos", new DateOnly(1941, 5, 18), "+351 910 000 002", null, new short[] { 2 }, Array.Empty<short>()),
            new[] { FicheirosTeste.Documento(FicheirosTeste.Png, "ReceitaMedica", "receita.png") },
            CancellationToken.None);

        Assert.Equal("Maria Santos", editada.Ficha.NomeCompleto);
        Assert.Equal("+351 910 000 002", editada.Ficha.ContactoEmergencia);
        Assert.Null(editada.Ficha.GrauAutonomiaId);
        Assert.Equal((short)2, Assert.Single(editada.Ficha.CondicoesSaude).Id);
        Assert.Empty(editada.Ficha.Alergias);
        Assert.Equal(CategoriaDocumento.ReceitaMedica, Assert.Single(this.repositorios.Documentos).Categoria);
    }

    /// <summary>
    /// RF-029 CA2 e RF-120: o Familiar não edita (403) e uma ficha arquivada não é editada (409).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarRecusaFamiliarEFichaArquivada()
    {
        var criada = await this.CriarAsync(Dados());
        var familiar = Guid.NewGuid();
        criada.Ficha.Associar(familiar, Papel.Familiar, Agora);

        var doFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.EditarAsync(familiar, criada.Ficha.Id, Dados(), Array.Empty<DocumentoSubmetido>(), CancellationToken.None));
        criada.Ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.EditarAsync(this.criador, criada.Ficha.Id, Dados(), Array.Empty<DocumentoSubmetido>(), CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
    }

    /// <summary>
    /// RF-023 CA3 e RNF-046: se o documento acrescentado não for guardado, a ficha fica como estava.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EditarComFalhaNoDocumentoNaoAlteraNada()
    {
        var unidade = new UnidadeDeTrabalhoComReversao(this.repositorios);
        var servicoComRegisto = this.ServicoCom(unidade);
        var criada = await servicoComRegisto.CriarAsync(this.criador, Dados(), false, Eu, Array.Empty<DocumentoSubmetido>(), CancellationToken.None);
        this.armazenamento.GravacoesAntesDeFalhar = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => servicoComRegisto.EditarAsync(
                this.criador,
                criada.Ficha.Id,
                Dados() with { NomeCompleto = "Outro Nome" },
                new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf) },
                CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoNaoGuardado, erro.Codigo);
        Assert.Equal("Maria Fictícia Santos", Assert.Single(this.repositorios.Fichas).NomeCompleto);
        Assert.Empty(this.repositorios.Documentos);
    }

    /// <summary>
    /// RF-120 e RF-122: o Cuidador arquiva a ficha e as tomas e execuções futuras por confirmar são apagadas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ArquivarApagaOsEventosFuturos()
    {
        var criada = await this.CriarAsync(Dados());

        await this.servico.ArquivarAsync(this.criador, criada.Ficha.Id, CancellationToken.None);

        Assert.Equal(EstadoFicha.Arquivada, criada.Ficha.Estado);
        Assert.Equal((criada.Ficha.Id, Agora), Assert.Single(this.repositorios.FuturasEliminadas));
    }

    /// <summary>
    /// RF-120 CA2: o Familiar não arquiva nem reativa a ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoArquivaNemReativa()
    {
        var criada = await this.CriarAsync(Dados());
        var familiar = Guid.NewGuid();
        criada.Ficha.Associar(familiar, Papel.Familiar, Agora);

        var arquivar = await Assert.ThrowsAsync<DominioException>(() => this.servico.ArquivarAsync(familiar, criada.Ficha.Id, CancellationToken.None));
        var reativar = await Assert.ThrowsAsync<DominioException>(() => this.servico.ReativarAsync(familiar, criada.Ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, arquivar.Codigo);
        Assert.Equal(CodigosErro.AcessoProibido, reativar.Codigo);
        Assert.True(criada.Ficha.EstaAtiva());
    }

    /// <summary>
    /// RF-121: o Cuidador reativa a ficha arquivada; a geração retoma a partir desse instante.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ReativarVoltaAoEstadoAtivo()
    {
        var criada = await this.CriarAsync(Dados());
        await this.servico.ArquivarAsync(this.criador, criada.Ficha.Id, CancellationToken.None);
        this.relogio.Agora = Agora.AddDays(3);

        await this.servico.ReativarAsync(this.criador, criada.Ficha.Id, CancellationToken.None);

        Assert.True(criada.Ficha.EstaAtiva());
        Assert.Equal(Agora.AddDays(3), criada.Ficha.AtivaDesdeUtc);
    }

    /// <summary>
    /// RF-123 CA1: a ficha sem registos é eliminada, com as notificações e os ficheiros dos documentos.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task EliminarFichaSemRegistos()
    {
        var criada = await this.CriarAsync(Dados(), documentos: new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf) });
        var caminho = Assert.Single(this.repositorios.Documentos).CaminhoRelativo;
        criada.Ficha.Arquivar(Agora);

        await this.servico.EliminarAsync(this.criador, criada.Ficha.Id, CancellationToken.None);

        Assert.Empty(this.repositorios.Fichas);
        Assert.Empty(this.repositorios.Documentos);
        Assert.Equal(criada.Ficha.Id, Assert.Single(this.repositorios.NotificacoesEliminadas));
        Assert.Equal(caminho, Assert.Single(this.armazenamento.Eliminados));
        Assert.Empty(this.armazenamento.Ficheiros);
    }

    /// <summary>
    /// RF-123 CA2: com medicação, tarefas ou histórico, a eliminação é recusada e nada é removido.
    /// </summary>
    /// <param name="registo">Tipo de registo da ficha.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("medicacao")]
    [InlineData("tarefas")]
    [InlineData("historico")]
    public async Task EliminarFichaComRegistosERecusado(string registo)
    {
        var criada = await this.CriarAsync(Dados(), documentos: new[] { FicheirosTeste.Documento(FicheirosTeste.Pdf) });
        var conjunto = registo switch
        {
            "medicacao" => this.repositorios.FichasComMedicacao,
            "tarefas" => this.repositorios.FichasComTarefas,
            _ => this.repositorios.FichasComHistorico,
        };
        conjunto.Add(criada.Ficha.Id);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.EliminarAsync(this.criador, criada.Ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.FichaComHistorico, erro.Codigo);
        Assert.Single(this.repositorios.Fichas);
        Assert.Single(this.armazenamento.Ficheiros);
        Assert.Empty(this.armazenamento.Eliminados);
        Assert.Empty(this.repositorios.NotificacoesEliminadas);
    }

    /// <summary>
    /// O Familiar não elimina a ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoElimina()
    {
        var criada = await this.CriarAsync(Dados());
        var familiar = Guid.NewGuid();
        criada.Ficha.Associar(familiar, Papel.Familiar, Agora);

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.EliminarAsync(familiar, criada.Ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, erro.Codigo);
        Assert.Single(this.repositorios.Fichas);
    }

    /// <summary>
    /// OP-08: devolve as três listas fechadas, com os graus pela ordem de apresentação.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ObterDominiosDevolveAsTresListas()
    {
        var dominios = await this.servico.ObterDominiosAsync(CancellationToken.None);

        Assert.Equal(2, dominios.CondicoesSaude.Count);
        Assert.Equal(2, dominios.Alergias.Count);
        Assert.Equal(new short[] { 1, 2 }, dominios.GrausAutonomia.Select(g => g.Id).ToArray());
    }

    private static DadosFicha Dados(short? grau = null, short[]? condicoes = null, short[]? alergias = null)
    {
        return new DadosFicha("  Maria Fictícia Santos  ", new DateOnly(1941, 5, 17), "+351 910 000 001", grau, condicoes, alergias);
    }

    private Task<AcessoFicha> CriarAsync(
        DadosFicha dados,
        bool propria = false,
        DefinicaoPrincipal? principal = null,
        IReadOnlyList<DocumentoSubmetido>? documentos = null)
    {
        return this.servico.CriarAsync(
            this.criador,
            dados,
            propria,
            propria ? principal : principal ?? Eu,
            documentos ?? Array.Empty<DocumentoSubmetido>(),
            CancellationToken.None);
    }

    private FichaService ServicoCom(UnidadeDeTrabalhoComReversao unidade)
    {
        var autorizacao = new AutorizacaoFichaService(this.repositorios);
        var documentos = new DocumentoService(this.repositorios, this.armazenamento, autorizacao, unidade, this.relogio);
        return new FichaService(
            this.repositorios,
            this.repositorios,
            new ConviteService(this.repositorios, this.repositorios, this.repositorios, autorizacao, unidade, this.relogio),
            this.repositorios,
            this.repositorios,
            documentos,
            autorizacao,
            unidade,
            this.relogio);
    }
}
