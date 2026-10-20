// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.UnitTests.Suporte;

namespace DoseCerta.UnitTests.Aplicacao.Documentos;

/// <summary>
/// Testes unitários de <see cref="DocumentoService"/> (RF-022, RF-023, RF-124, RNF-007, RNF-008).
/// </summary>
public class DocumentoServiceTestes
{
    private static readonly DateTime Agora = new(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string[] CaminhosAEliminar = { "a.pdf", "b.png" };

    private readonly RepositoriosEmMemoria repositorios = new();
    private readonly ArmazenamentoEmMemoria armazenamento = new();
    private readonly UnidadeDeTrabalhoEmMemoria unidadeDeTrabalho = new();
    private readonly DocumentoService servico;
    private readonly Ficha ficha;
    private readonly Guid cuidador = Guid.NewGuid();
    private readonly Guid familiar = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentoServiceTestes"/> class.
    /// </summary>
    public DocumentoServiceTestes()
    {
        this.servico = new DocumentoService(
            this.repositorios,
            this.armazenamento,
            new AutorizacaoFichaService(this.repositorios),
            this.unidadeDeTrabalho,
            new RelogioFixo(Agora));
        this.ficha = new Ficha("Utente Fictício", new DateOnly(1941, 5, 17), "+351 910 000 001", null, this.cuidador, Agora);
        this.ficha.AssociarCuidadorPrincipal(this.cuidador, provisorio: false, Agora);
        this.ficha.Associar(this.familiar, Papel.Familiar, Agora);
        this.repositorios.Fichas.Add(this.ficha);
    }

    /// <summary>
    /// O formato é detetado pelo conteúdo do ficheiro, não pela extensão do nome.
    /// </summary>
    /// <param name="tipo">Ficheiro de teste.</param>
    /// <param name="esperado">Formato esperado.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("pdf", FormatoDocumento.PDF)]
    [InlineData("png", FormatoDocumento.PNG)]
    [InlineData("jpg", FormatoDocumento.JPG)]
    public async Task ValidarDetetaOFormatoPeloConteudo(string tipo, FormatoDocumento esperado)
    {
        var conteudo = tipo switch
        {
            "pdf" => FicheirosTeste.Pdf,
            "png" => FicheirosTeste.Png,
            _ => FicheirosTeste.Jpg,
        };

        var validado = await DocumentoService.ValidarAsync(FicheirosTeste.Documento(conteudo, nome: "ficheiro.txt"), string.Empty, CancellationToken.None);

        Assert.Equal(esperado, validado.Formato);
        Assert.Equal(CategoriaDocumento.RelatorioClinico, validado.Categoria);
        Assert.Equal("ficheiro.txt", validado.NomeOriginal);
    }

    /// <summary>
    /// RF-022 CA2: uma categoria diferente das três previstas, ou em falta, é recusada com 400 e o campo indicado.
    /// </summary>
    /// <param name="categoria">Categoria enviada.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Theory]
    [InlineData("Receita")]
    [InlineData("1")]
    [InlineData(null)]
    public async Task CategoriaInvalidaERecusada(string? categoria)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => DocumentoService.ValidarAsync(FicheirosTeste.Documento(FicheirosTeste.Pdf, categoria), "documentos[0]", CancellationToken.None));

        Assert.Equal("documentos[0].categoria", Assert.Single(erro.Campos).Campo);
    }

    /// <summary>
    /// Um ficheiro em falta ou vazio é recusado com 400.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FicheiroEmFaltaOuVazioERecusado()
    {
        var emFalta = await Assert.ThrowsAsync<ValidacaoException>(
            () => DocumentoService.ValidarAsync(new DocumentoSubmetido("ReceitaMedica", null, 0, null), string.Empty, CancellationToken.None));
        var vazio = await Assert.ThrowsAsync<ValidacaoException>(
            () => DocumentoService.ValidarAsync(FicheirosTeste.Documento(Array.Empty<byte>()), string.Empty, CancellationToken.None));

        Assert.Equal("ficheiro", Assert.Single(emFalta.Campos).Campo);
        Assert.Equal("ficheiro", Assert.Single(vazio.Campos).Campo);
    }

    /// <summary>
    /// RNF-008: mais de 10 MB dá 413 e um formato diferente de PDF, JPG ou PNG dá 415.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task TamanhoEFormatoSaoVerificados()
    {
        var grande = await Assert.ThrowsAsync<DominioException>(
            () => DocumentoService.ValidarAsync(
                FicheirosTeste.Documento(FicheirosTeste.Pdf, tamanho: ParametrosDeNegocio.TamanhoMaximoDocumento + 1L),
                string.Empty,
                CancellationToken.None));
        var formato = await Assert.ThrowsAsync<DominioException>(
            () => DocumentoService.ValidarAsync(FicheirosTeste.Documento(FicheirosTeste.Texto, nome: "falso.pdf"), string.Empty, CancellationToken.None));

        Assert.Equal(CodigosErro.FicheiroDemasiadoGrande, grande.Codigo);
        Assert.Equal(CodigosErro.FormatoNaoSuportado, formato.Codigo);
    }

    /// <summary>
    /// O nome original perde o caminho e é limitado a 255 caracteres; sem nome, recebe um nome genérico.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NomeOriginalENormalizado()
    {
        var comCaminho = await DocumentoService.ValidarAsync(FicheirosTeste.Documento(FicheirosTeste.Pdf, nome: "pasta/relatorio.pdf"), string.Empty, CancellationToken.None);
        var semNome = await DocumentoService.ValidarAsync(FicheirosTeste.Documento(FicheirosTeste.Png, nome: null), string.Empty, CancellationToken.None);
        var longo = await DocumentoService.ValidarAsync(FicheirosTeste.Documento(FicheirosTeste.Pdf, nome: new string('a', 300) + ".pdf"), string.Empty, CancellationToken.None);

        Assert.Equal("relatorio.pdf", comCaminho.NomeOriginal);
        Assert.Equal("documento.png", semNome.NomeOriginal);
        Assert.Equal(DocumentoService.ComprimentoMaximoNome, longo.NomeOriginal.Length);
        Assert.EndsWith(".pdf", longo.NomeOriginal, StringComparison.Ordinal);
    }

    /// <summary>
    /// OP-15: o Cuidador carrega um documento; o ficheiro fica guardado e os metadados registados (RF-022 CA1).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CarregarGuardaFicheiroEMetadados()
    {
        var documento = await this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None);

        Assert.Same(documento, Assert.Single(this.repositorios.Documentos));
        Assert.True(this.armazenamento.Ficheiros.ContainsKey(documento.CaminhoRelativo));
        Assert.Equal(this.ficha.Id, documento.FichaId);
        Assert.Equal(this.cuidador, documento.CarregadoPorId);
        Assert.Equal(Agora, documento.CarregadoEmUtc);
        Assert.Equal(FicheirosTeste.Pdf.Length, documento.TamanhoBytes);
        Assert.Null(documento.TratamentoId);
    }

    /// <summary>
    /// RF-023 CA3: se o ficheiro não puder ser guardado, nada é registado e a resposta é 503.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaAoGuardarNaoDeixaReferencias()
    {
        this.armazenamento.GravacoesAntesDeFalhar = 0;

        var erro = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoNaoGuardado, erro.Codigo);
        Assert.Empty(this.repositorios.Documentos);
        Assert.Empty(this.armazenamento.Ficheiros);
    }

    /// <summary>
    /// RNF-046: se a transação falhar depois de guardar o ficheiro, o ficheiro é apagado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FalhaDaTransacaoApagaOFicheiro()
    {
        this.unidadeDeTrabalho.AntesDeConfirmar = () => throw new InvalidOperationException("Base de dados indisponível (simulado).");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None));

        Assert.Empty(this.armazenamento.Ficheiros);
        Assert.Single(this.armazenamento.Eliminados);
        Assert.Equal(1, this.unidadeDeTrabalho.CompensacoesExecutadas);
    }

    /// <summary>
    /// RF-048 e RP-13: o Familiar não carrega documentos e uma ficha arquivada não os aceita.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarEFichaArquivadaSaoRecusados()
    {
        var doFamiliar = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.CarregarAsync(this.familiar, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None));
        this.ficha.Arquivar(Agora);
        var arquivada = await Assert.ThrowsAsync<DominioException>(
            () => this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None));

        Assert.Equal(CodigosErro.AcessoProibido, doFamiliar.Codigo);
        Assert.Equal(CodigosErro.FichaArquivada, arquivada.Codigo);
        Assert.Empty(this.armazenamento.Ficheiros);
    }

    /// <summary>
    /// OP-16 e OP-17: o Familiar lista e descarrega os documentos da ficha (RF-028).
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarListaEDescarrega()
    {
        var documento = await this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None);

        var lista = await this.servico.ListarAsync(this.familiar, this.ficha.Id, CancellationToken.None);
        var lido = await this.servico.ObterAsync(this.familiar, documento.Id, CancellationToken.None);

        Assert.Same(documento, Assert.Single(lista));
        Assert.Same(documento, lido.Documento);
        await using var conteudo = lido.Conteudo;
        using var copia = new MemoryStream();
        await conteudo.CopyToAsync(copia);
        Assert.Equal(FicheirosTeste.Pdf, copia.ToArray());
    }

    /// <summary>
    /// RNF-007: um documento inexistente ou de uma ficha a que o Utilizador não está associado dá a mesma resposta 404.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DocumentoDeOutraFichaDa404()
    {
        var documento = await this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None);

        var naoAssociado = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(Guid.NewGuid(), documento.Id, CancellationToken.None));
        var inexistente = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(this.cuidador, Guid.NewGuid(), CancellationToken.None));
        var lista = await Assert.ThrowsAsync<DominioException>(() => this.servico.ListarAsync(Guid.NewGuid(), this.ficha.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoNaoEncontrado, naoAssociado.Codigo);
        Assert.Equal(CodigosErro.DocumentoNaoEncontrado, inexistente.Codigo);
        Assert.Equal(CodigosErro.FichaNaoEncontrada, lista.Codigo);
    }

    /// <summary>
    /// RF-124: se o ficheiro guardado não puder ser obtido, a resposta é 503 e os metadados mantêm-se.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FicheiroIlegivelDa503()
    {
        var documento = await this.servico.CarregarAsync(this.cuidador, this.ficha.Id, FicheirosTeste.Documento(FicheirosTeste.Pdf), CancellationToken.None);
        this.armazenamento.FalharLeitura = true;

        var erro = await Assert.ThrowsAsync<DominioException>(() => this.servico.ObterAsync(this.cuidador, documento.Id, CancellationToken.None));

        Assert.Equal(CodigosErro.DocumentoIndisponivel, erro.Codigo);
        Assert.Single(this.repositorios.Documentos);
    }

    /// <summary>
    /// RF-123: os ficheiros de uma ficha eliminada são removidos do armazenamento.
    /// </summary>
    [Fact]
    public void EliminarFicheirosRemoveTodos()
    {
        this.servico.EliminarFicheiros(CaminhosAEliminar);

        Assert.Equal(CaminhosAEliminar, this.armazenamento.Eliminados);
    }
}
