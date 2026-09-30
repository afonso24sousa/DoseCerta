// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dados;
using DoseCerta.Api.Dominio;
using DoseCerta.Api.Servicos;
using DoseCerta.Api.Tests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DoseCerta.Api.Tests.Servicos;

/// <summary>
/// Testes das regras de negócio da ficha própria (I003 / F003).
/// </summary>
public sealed class ServicoFichasTestes : IDisposable
{
    private readonly BaseDadosTeste baseDados = new();
    private readonly ArmazenamentoEmMemoria armazenamento = new();
    private readonly Mock<IVerificadorDependenciasFicha> verificador = new();

    /// <summary>
    /// Criação com sucesso: a ficha fica marcada como própria e o Utilizador fica Cuidador principal, sem convite.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task CriarFichaPropriaAssociaUtilizadorComoCuidadorPrincipal()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        Assert.True(ficha.EhFichaPropria);
        Assert.Equal(BaseDadosTeste.Ana, ficha.UtilizadorProprietarioId);
        Assert.Equal(EstadoFicha.Ativa, ficha.Estado);
        var associacao = Assert.Single(ficha.Associacoes);
        Assert.Equal(BaseDadosTeste.Ana, associacao.UtilizadorId);
        Assert.Equal(PapelFicha.Cuidador, associacao.Papel);
        Assert.True(associacao.EhCuidadorPrincipal);
        Assert.Equal("Hipertensão arterial", Assert.Single(ficha.CondicoesSaude).Nome);
        Assert.Equal("Penicilina e derivados", Assert.Single(ficha.Alergias).Nome);
        Assert.Equal("Autónomo", ficha.GrauAutonomia?.Nome);
    }

    /// <summary>
    /// Um Utilizador que já tem ficha própria não consegue criar uma segunda (erro de negócio claro).
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task SegundaFichaPropriaEhRecusada()
    {
        await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarFichaPropriaAsync(BaseDadosTeste.Ana));

        Assert.Equal(CodigoErro.FichaPropriaJaExiste, erro.Codigo);
        using var contexto = this.baseDados.CriarContexto();
        Assert.Equal(1, await contexto.Fichas.CountAsync(f => f.UtilizadorProprietarioId == BaseDadosTeste.Ana));
    }

    /// <summary>
    /// A restrição também existe na base de dados: o índice único trava uma segunda ficha própria
    /// mesmo que a validação da aplicação seja contornada.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task IndiceUnicoDaBaseDeDadosImpedeSegundaFichaPropria()
    {
        using var contexto = this.baseDados.CriarContexto();
        var pedido = Pedidos.FichaPropria();
        contexto.Fichas.Add(Ficha.CriarPropria(BaseDadosTeste.Ana, pedido.Identificacao, Array.Empty<CondicaoSaude>(), Array.Empty<Alergia>(), DateTime.UtcNow));
        await contexto.SaveChangesAsync();

        contexto.Fichas.Add(Ficha.CriarPropria(BaseDadosTeste.Ana, pedido.Identificacao, Array.Empty<CondicaoSaude>(), Array.Empty<Alergia>(), DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    /// <summary>
    /// Cada Utilizador tem a sua ficha própria: a restrição é por Utilizador, não global.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task UtilizadoresDiferentesTemCadaUmASuaFichaPropria()
    {
        await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);
        await this.CriarFichaPropriaAsync(BaseDadosTeste.Bruno);

        using var contexto = this.baseDados.CriarContexto();
        Assert.Equal(2, await contexto.Fichas.CountAsync());
    }

    /// <summary>
    /// Os campos obrigatórios e as categorias fechadas são validados no servidor.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task CamposObrigatoriosECategoriasFechadasSaoValidadosNoServidor()
    {
        var pedido = new NovaFicha(
            ParaOUtilizadorAutenticado: true,
            new DadosIdentificacaoFicha(" ", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), string.Empty, string.Empty, 99),
            new List<int>(),
            new List<int> { 999 },
            Array.Empty<NovoDocumento>());

        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => this.CriarServico().CriarAsync(BaseDadosTeste.Ana, pedido, CancellationToken.None));

        Assert.Contains("nomeCompleto", erro.Erros.Keys);
        Assert.Contains("dataNascimento", erro.Erros.Keys);
        Assert.Contains("contactoEmergenciaNome", erro.Erros.Keys);
        Assert.Contains("contactoEmergenciaTelefone", erro.Erros.Keys);
        Assert.Contains("grauAutonomiaId", erro.Erros.Keys);
        Assert.Contains("condicoesSaudeIds", erro.Erros.Keys);
        Assert.Contains("alergiasIds", erro.Erros.Keys);
        await this.AssegurarQueNaoExistemDadosAsync();
    }

    /// <summary>
    /// As opções de ausência ("Sem alergias conhecidas", etc.) não podem ser combinadas com outras.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task OpcaoDeAusenciaNaoPodeSerCombinadaComOutras()
    {
        var pedido = Pedidos.FichaPropria() with
        {
            CondicoesSaudeIds = new List<int> { Pedidos.SemCondicoes, Pedidos.Hipertensao },
            AlergiasIds = new List<int> { Pedidos.SemAlergias, Pedidos.Penicilina },
        };

        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => this.CriarServico().CriarAsync(BaseDadosTeste.Ana, pedido, CancellationToken.None));

        Assert.Contains("condicoesSaudeIds", erro.Erros.Keys);
        Assert.Contains("alergiasIds", erro.Erros.Keys);
    }

    /// <summary>
    /// Só são admitidos ficheiros PDF, JPEG ou PNG.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task TipoDeFicheiroNaoAdmitidoEhRecusado()
    {
        var executavel = new NovoDocumento(CategoriaDocumento.RelatorioClinico, "relatorio.exe", "application/octet-stream", 10, () => new MemoryStream(new byte[10]));

        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => this.CriarServico().CriarAsync(BaseDadosTeste.Ana, Pedidos.FichaPropria(executavel), CancellationToken.None));

        Assert.Contains("documentos[0].ficheiro", erro.Erros.Keys);
        Assert.Empty(this.armazenamento.Chaves);
    }

    /// <summary>
    /// Documentos gravados com sucesso ficam associados à ficha, com a chave do ficheiro guardado.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task DocumentosGravadosFicamAssociadosAFicha()
    {
        var ficha = await this.CriarFichaPropriaAsync(
            BaseDadosTeste.Ana,
            Pedidos.Pdf(CategoriaDocumento.ReceitaMedica, "receita.pdf"),
            Pedidos.Pdf(CategoriaDocumento.DocumentoIdentificacao, "cc.pdf"));

        Assert.Equal(2, ficha.Documentos.Count);
        Assert.All(ficha.Documentos, d => Assert.Contains(d.ChaveArmazenamento, this.armazenamento.Chaves));
        Assert.Contains(ficha.Documentos, d => d.Categoria == CategoriaDocumento.DocumentoIdentificacao && d.NomeOriginal == "cc.pdf");
    }

    /// <summary>
    /// Se a gravação de um ficheiro falhar, a operação inteira é abortada: não fica ficha, associação,
    /// documento nem ficheiro órfão; o mesmo pedido pode ser repetido com sucesso.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaNaGravacaoDeUmDocumentoAbortaTodaAOperacao()
    {
        var pedido = Pedidos.FichaPropria(
            Pedidos.Pdf(CategoriaDocumento.ReceitaMedica, "receita.pdf"),
            Pedidos.Pdf(CategoriaDocumento.RelatorioClinico, "relatorio.pdf"));
        this.armazenamento.FalharAposGravacoes = 1;

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().CriarAsync(BaseDadosTeste.Ana, pedido, CancellationToken.None));

        Assert.Equal(CodigoErro.FalhaGravacaoDocumento, erro.Codigo);
        Assert.Contains("relatorio.pdf", erro.Message, StringComparison.Ordinal);
        await this.AssegurarQueNaoExistemDadosAsync();
        Assert.Empty(this.armazenamento.Chaves);

        this.armazenamento.FalharAposGravacoes = null;
        var ficha = await this.CriarServico().CriarAsync(BaseDadosTeste.Ana, pedido, CancellationToken.None);
        Assert.Equal(2, ficha.Documentos.Count);
    }

    /// <summary>
    /// A criação de fichas de terceiro pertence à I002 e é recusada com um erro explícito.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaDeTerceiroFicaParaAI002()
    {
        var pedido = Pedidos.FichaPropria() with { ParaOUtilizadorAutenticado = false };

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().CriarAsync(BaseDadosTeste.Ana, pedido, CancellationToken.None));

        Assert.Equal(CodigoErro.NaoImplementado, erro.Codigo);
    }

    /// <summary>
    /// Na ficha própria, outro Cuidador não pode despromover o dono a Familiar.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task OutroCuidadorNaoPodeDespromoverODonoDaFichaPropria()
    {
        var fichaId = await this.CriarFichaPropriaComConvidadoAsync(BaseDadosTeste.Bruno, PapelFicha.Cuidador);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => this.CriarServico().AlterarPapelAsync(BaseDadosTeste.Bruno, fichaId, BaseDadosTeste.Ana, PapelFicha.Familiar, CancellationToken.None));

        Assert.Equal(CodigoErro.CuidadorPrincipalFichaPropriaImutavel, erro.Codigo);
        await this.AssegurarQueDonoContinuaCuidadorPrincipalAsync(fichaId);
    }

    /// <summary>
    /// Na ficha própria, outro Cuidador não pode remover o dono.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task OutroCuidadorNaoPodeRemoverODonoDaFichaPropria()
    {
        var fichaId = await this.CriarFichaPropriaComConvidadoAsync(BaseDadosTeste.Bruno, PapelFicha.Cuidador);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => this.CriarServico().RemoverAssociacaoAsync(BaseDadosTeste.Bruno, fichaId, BaseDadosTeste.Ana, CancellationToken.None));

        Assert.Equal(CodigoErro.CuidadorPrincipalFichaPropriaImutavel, erro.Codigo);
        await this.AssegurarQueDonoContinuaCuidadorPrincipalAsync(fichaId);
    }

    /// <summary>
    /// O próprio dono também não deixa de ser Cuidador principal da sua ficha.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task DonoNaoPodeDespromoverSeASiProprio()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => this.CriarServico().AlterarPapelAsync(BaseDadosTeste.Ana, ficha.Id, BaseDadosTeste.Ana, PapelFicha.Familiar, CancellationToken.None));

        Assert.Equal(CodigoErro.CuidadorPrincipalFichaPropriaImutavel, erro.Codigo);
    }

    /// <summary>
    /// Segunda linha de defesa: mesmo alterando diretamente a associação no contexto (sem passar pelo
    /// domínio), a gravação é recusada.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task AlteracaoDiretaDaAssociacaoDoDonoEhRecusadaAoGravar()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        using (var contexto = this.baseDados.CriarContexto())
        {
            var associacao = await contexto.AssociacoesFicha.SingleAsync(a => a.FichaId == ficha.Id && a.UtilizadorId == BaseDadosTeste.Ana);
            associacao.Papel = PapelFicha.Familiar;
            associacao.EhCuidadorPrincipal = false;

            var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => contexto.SaveChangesAsync());
            Assert.Equal(CodigoErro.CuidadorPrincipalFichaPropriaImutavel, erro.Codigo);
        }

        using (var contexto = this.baseDados.CriarContexto())
        {
            var associacao = await contexto.AssociacoesFicha.SingleAsync(a => a.FichaId == ficha.Id && a.UtilizadorId == BaseDadosTeste.Ana);
            contexto.AssociacoesFicha.Remove(associacao);

            var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => contexto.SaveChangesAsync());
            Assert.Equal(CodigoErro.CuidadorPrincipalFichaPropriaImutavel, erro.Codigo);
        }

        await this.AssegurarQueDonoContinuaCuidadorPrincipalAsync(ficha.Id);
    }

    /// <summary>
    /// O dono pode associar outras pessoas como Cuidador ou Familiar na ficha própria, e essas
    /// associações seguem as regras normais (podem ser alteradas e removidas).
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task DonoPodeAssociarCuidadoresEFamiliaresNaFichaPropria()
    {
        var fichaId = await this.CriarFichaPropriaComConvidadoAsync(BaseDadosTeste.Bruno, PapelFicha.Familiar);
        using (var contexto = this.baseDados.CriarContexto())
        {
            var ficha = await contexto.Fichas.Include(f => f.Associacoes).SingleAsync(f => f.Id == fichaId);
            ficha.AdicionarAssociacao(BaseDadosTeste.Carla, PapelFicha.Cuidador, DateTime.UtcNow);
            await contexto.SaveChangesAsync();
        }

        var servico = this.CriarServico();
        await servico.AlterarPapelAsync(BaseDadosTeste.Ana, fichaId, BaseDadosTeste.Bruno, PapelFicha.Cuidador, CancellationToken.None);
        await servico.RemoverAssociacaoAsync(BaseDadosTeste.Ana, fichaId, BaseDadosTeste.Carla, CancellationToken.None);

        var resultado = await this.CriarServico().ObterAsync(BaseDadosTeste.Bruno, fichaId, CancellationToken.None);
        Assert.Equal(2, resultado.Associacoes.Count);
        Assert.Contains(resultado.Associacoes, a => a.UtilizadorId == BaseDadosTeste.Bruno && a.Papel == PapelFicha.Cuidador && !a.EhCuidadorPrincipal);
        Assert.Contains(resultado.Associacoes, a => a.UtilizadorId == BaseDadosTeste.Ana && a.EhCuidadorPrincipal);
    }

    /// <summary>
    /// Um Familiar não pode gerir as pessoas associadas.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoPodeGerirAssociacoes()
    {
        var fichaId = await this.CriarFichaPropriaComConvidadoAsync(BaseDadosTeste.Bruno, PapelFicha.Familiar);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => this.CriarServico().RemoverAssociacaoAsync(BaseDadosTeste.Bruno, fichaId, BaseDadosTeste.Bruno, CancellationToken.None));

        Assert.Equal(CodigoErro.AcessoNegado, erro.Codigo);
    }

    /// <summary>
    /// Uma ficha sem medicação, tarefas nem histórico pode ser eliminada pelo dono; os ficheiros são removidos.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaSemDependenciasPodeSerEliminada()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana, Pedidos.Pdf());

        await this.CriarServico().EliminarAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None);

        await this.AssegurarQueNaoExistemDadosAsync();
        Assert.Empty(this.armazenamento.Chaves);
    }

    /// <summary>
    /// Uma ficha com medicação, tarefas ou histórico não pode ser eliminada, mas pode ser arquivada e reativada.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task FichaComDependenciasSoPodeSerArquivada()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);
        this.verificador.Setup(v => v.TemDependenciasAsync(ficha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().EliminarAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None));
        Assert.Equal(CodigoErro.FichaComHistorico, erro.Codigo);

        await this.CriarServico().ArquivarAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None);
        var arquivada = await this.CriarServico().ObterAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None);
        Assert.Equal(EstadoFicha.Arquivada, arquivada.Estado);
        Assert.NotNull(arquivada.DataArquivo);

        await this.CriarServico().ReativarAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None);
        var reativada = await this.CriarServico().ObterAsync(BaseDadosTeste.Ana, ficha.Id, CancellationToken.None);
        Assert.Equal(EstadoFicha.Ativa, reativada.Estado);
        Assert.Null(reativada.DataArquivo);
    }

    /// <summary>
    /// Na ficha própria, só o dono a pode eliminar ou arquivar, mesmo que existam outros Cuidadores.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task OutroCuidadorNaoPodeEliminarNemArquivarAFichaPropria()
    {
        var fichaId = await this.CriarFichaPropriaComConvidadoAsync(BaseDadosTeste.Bruno, PapelFicha.Cuidador);

        var eliminar = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().EliminarAsync(BaseDadosTeste.Bruno, fichaId, CancellationToken.None));
        var arquivar = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().ArquivarAsync(BaseDadosTeste.Bruno, fichaId, CancellationToken.None));

        Assert.Equal(CodigoErro.AcessoNegado, eliminar.Codigo);
        Assert.Equal(CodigoErro.AcessoNegado, arquivar.Codigo);
    }

    /// <summary>
    /// Um Utilizador não associado não consegue consultar a ficha.
    /// </summary>
    /// <returns>A tarefa assíncrona.</returns>
    [Fact]
    public async Task UtilizadorNaoAssociadoNaoConsultaAFicha()
    {
        var ficha = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => this.CriarServico().ObterAsync(BaseDadosTeste.Carla, ficha.Id, CancellationToken.None));

        Assert.Equal(CodigoErro.FichaNaoEncontrada, erro.Codigo);
    }

    /// <inheritdoc/>
    public void Dispose() => this.baseDados.Dispose();

    private ServicoFichas CriarServico() =>
        new(
            this.baseDados.CriarContexto(),
            this.armazenamento,
            this.verificador.Object,
            TimeProvider.System,
            NullLogger<ServicoFichas>.Instance);

    private Task<Ficha> CriarFichaPropriaAsync(Guid utilizadorId, params NovoDocumento[] documentos) =>
        this.CriarServico().CriarAsync(utilizadorId, Pedidos.FichaPropria(documentos), CancellationToken.None);

    /// <summary>
    /// Cria a ficha própria da Ana e associa outro Utilizador, como faria a F004 depois de aceite o convite.
    /// </summary>
    private async Task<Guid> CriarFichaPropriaComConvidadoAsync(Guid convidado, PapelFicha papel)
    {
        var criada = await this.CriarFichaPropriaAsync(BaseDadosTeste.Ana);

        using var contexto = this.baseDados.CriarContexto();
        var ficha = await contexto.Fichas.Include(f => f.Associacoes).SingleAsync(f => f.Id == criada.Id);
        ficha.AdicionarAssociacao(convidado, papel, DateTime.UtcNow);
        await contexto.SaveChangesAsync();
        return ficha.Id;
    }

    private async Task AssegurarQueDonoContinuaCuidadorPrincipalAsync(Guid fichaId)
    {
        using var contexto = this.baseDados.CriarContexto();
        var dono = await contexto.AssociacoesFicha.SingleAsync(a => a.FichaId == fichaId && a.UtilizadorId == BaseDadosTeste.Ana);
        Assert.Equal(PapelFicha.Cuidador, dono.Papel);
        Assert.True(dono.EhCuidadorPrincipal);
    }

    private async Task AssegurarQueNaoExistemDadosAsync()
    {
        using DoseCertaDbContext contexto = this.baseDados.CriarContexto();
        Assert.Equal(0, await contexto.Fichas.CountAsync());
        Assert.Equal(0, await contexto.AssociacoesFicha.CountAsync());
        Assert.Equal(0, await contexto.DocumentosFicha.CountAsync());
    }
}
