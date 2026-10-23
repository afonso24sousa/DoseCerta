// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Documentos;

/// <summary>
/// Validação, gravação e leitura dos documentos das fichas (F002). O ficheiro é guardado antes dos metadados e é apagado
/// se a transação falhar, para não ficarem referências a ficheiros inexistentes nem ficheiros órfãos
/// (RF-022, RF-023, RF-123, RF-124, RNF-006 a RNF-008, RNF-046, AD-10).
/// </summary>
public sealed class DocumentoService
{
    /// <summary>
    /// Comprimento máximo do nome original guardado (modelo de dados).
    /// </summary>
    public const int ComprimentoMaximoNome = 255;

    private const string MensagemNaoEncontrado = "O documento não existe ou não pertence a uma ficha a que esteja associado.";

    private static readonly byte[] AssinaturaPdf = "%PDF-"u8.ToArray();
    private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] AssinaturaJpg = { 0xFF, 0xD8, 0xFF };

    private readonly IDocumentoRepositorio documentos;
    private readonly IArmazenamentoDocumentos armazenamento;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IUnidadeDeTrabalho unidadeDeTrabalho;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentoService"/> class.
    /// </summary>
    /// <param name="documentos">Repositório dos documentos.</param>
    /// <param name="armazenamento">Armazenamento dos ficheiros.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    /// <param name="unidadeDeTrabalho">Transação de cada operação de escrita.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    public DocumentoService(
        IDocumentoRepositorio documentos,
        IArmazenamentoDocumentos armazenamento,
        AutorizacaoFichaService autorizacao,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio)
    {
        this.documentos = documentos;
        this.armazenamento = armazenamento;
        this.autorizacao = autorizacao;
        this.unidadeDeTrabalho = unidadeDeTrabalho;
        this.relogio = relogio;
    }

    /// <summary>
    /// Valida um documento: categoria entre as três previstas (400), até 10 MB (413) e formato PDF, JPG ou PNG
    /// detetado pelo conteúdo e não pela extensão (415) (RF-022, RNF-008).
    /// </summary>
    /// <param name="documento">Documento recebido.</param>
    /// <param name="prefixoCampo">Prefixo dos campos nos erros de validação, por exemplo <c>documentos[0]</c>; vazio para OP-15.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O documento validado.</returns>
    /// <exception cref="ValidacaoException">Categoria em falta ou inválida, ou ficheiro em falta ou vazio.</exception>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FicheiroDemasiadoGrande"/> ou <see cref="CodigosErro.FormatoNaoSuportado"/>.</exception>
    public static async Task<DocumentoValidado> ValidarAsync(DocumentoSubmetido documento, string prefixoCampo, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        var validador = new ValidadorDados();
        var categoria = LerCategoria(documento.Categoria);
        if (categoria is null)
        {
            validador.Erro(
                Campo(prefixoCampo, "categoria"),
                "É obrigatória e tem de ser ReceitaMedica, RelatorioClinico ou DocumentoIdentificacao.");
        }

        if (documento.AbrirLeitura is null || documento.TamanhoBytes <= 0)
        {
            validador.Erro(Campo(prefixoCampo, "ficheiro"), "É obrigatório e não pode estar vazio.");
        }

        validador.LancarSeInvalido();

        if (documento.TamanhoBytes > ParametrosDeNegocio.TamanhoMaximoDocumento)
        {
            throw new DominioException(CodigosErro.FicheiroDemasiadoGrande, "O ficheiro tem mais de 10 MB; o tamanho máximo é 10 MB.");
        }

        var formato = await DetetarFormatoAsync(documento.AbrirLeitura!, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.FormatoNaoSuportado, "O formato do ficheiro não é suportado: use PDF, JPG ou PNG.");

        return new DocumentoValidado(
            categoria!.Value,
            formato,
            NomeOriginal(documento.NomeOriginal, formato),
            (int)documento.TamanhoBytes,
            documento.AbrirLeitura!);
    }

    /// <summary>
    /// Valida todos os documentos de uma criação ou edição de ficha, antes de guardar qualquer ficheiro.
    /// </summary>
    /// <param name="submetidos">Documentos recebidos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os documentos validados, pela mesma ordem.</returns>
    public static async Task<IReadOnlyList<DocumentoValidado>> ValidarTodosAsync(IReadOnlyList<DocumentoSubmetido> submetidos, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(submetidos);

        var validados = new List<DocumentoValidado>(submetidos.Count);
        for (var i = 0; i < submetidos.Count; i++)
        {
            validados.Add(await ValidarAsync(submetidos[i], $"documentos[{i}]", cancelamento).ConfigureAwait(false));
        }

        return validados;
    }

    /// <summary>
    /// Guarda o ficheiro de um documento já validado e regista os metadados, dentro da transação em curso.
    /// O ficheiro é guardado primeiro e é apagado se a transação falhar (RNF-046).
    /// </summary>
    /// <param name="fichaId">Ficha do documento.</param>
    /// <param name="tratamentoId">Tratamento da receita (RF-052), ou <see langword="null"/> nos documentos da ficha.</param>
    /// <param name="documento">Documento validado.</param>
    /// <param name="autorId">Utilizador que carrega o documento.</param>
    /// <param name="agoraUtc">Instante do carregamento, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os metadados do documento.</returns>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.DocumentoNaoGuardado"/>, se o ficheiro não puder ser guardado (RF-023, RF-053).
    /// </exception>
    public async Task<Documento> GuardarNaTransacaoAsync(
        Guid fichaId,
        Guid? tratamentoId,
        DocumentoValidado documento,
        Guid autorId,
        DateTime agoraUtc,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        string caminho;
        try
        {
            var conteudo = documento.AbrirLeitura();
            await using (conteudo.ConfigureAwait(false))
            {
                caminho = await this.armazenamento.GuardarAsync(conteudo, documento.Formato, cancelamento).ConfigureAwait(false);
            }
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            throw new DominioException(
                CodigosErro.DocumentoNaoGuardado,
                "O documento não foi guardado; nenhum dado foi gravado. Tente novamente.");
        }

        this.unidadeDeTrabalho.AoFalhar(() => this.armazenamento.Eliminar(caminho));

        var registo = new Documento(
            fichaId,
            tratamentoId,
            documento.Categoria,
            documento.NomeOriginal,
            caminho,
            documento.Formato,
            documento.TamanhoBytes,
            autorId,
            agoraUtc);
        this.documentos.Adicionar(registo);
        return registo;
    }

    /// <summary>
    /// OP-15: carrega um documento numa ficha ativa; só um Cuidador o pode fazer (RF-022, RF-023).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha do documento.</param>
    /// <param name="submetido">Documento recebido.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os metadados do documento guardado.</returns>
    public async Task<Documento> CarregarAsync(Guid utilizadorId, Guid fichaId, DocumentoSubmetido submetido, CancellationToken cancelamento)
    {
        return await this.unidadeDeTrabalho.ExecutarEmTransacaoAsync(
            async () =>
            {
                await this.autorizacao.ExigirCuidadorDeFichaAtivaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
                var validado = await ValidarAsync(submetido, string.Empty, cancelamento).ConfigureAwait(false);
                return await this.GuardarNaTransacaoAsync(fichaId, null, validado, utilizadorId, this.relogio.AgoraUtc(), cancelamento)
                    .ConfigureAwait(false);
            },
            cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-16: lista os metadados dos documentos de uma ficha a Cuidadores e Familiares associados (RF-028, RNF-007).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os documentos da ficha.</returns>
    public async Task<IReadOnlyList<Documento>> ListarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        return await this.documentos.DaFichaAsync(fichaId, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-17: obtém o ficheiro de um documento, só para Utilizadores associados à ficha (RNF-007).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="documentoId">Documento pedido.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os metadados e o conteúdo do ficheiro.</returns>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.DocumentoNaoEncontrado"/> (404) ou <see cref="CodigosErro.DocumentoIndisponivel"/> (503, RF-124).
    /// </exception>
    public async Task<FicheiroLido> ObterAsync(Guid utilizadorId, Guid documentoId, CancellationToken cancelamento)
    {
        var documento = await this.documentos.ObterPorIdAsync(documentoId, cancelamento).ConfigureAwait(false);
        if (documento is null
            || await this.autorizacao.ObterAcessoAsync(utilizadorId, documento.FichaId, cancelamento).ConfigureAwait(false) is null)
        {
            throw new DominioException(CodigosErro.DocumentoNaoEncontrado, MensagemNaoEncontrado);
        }

        try
        {
            return new FicheiroLido(documento, this.armazenamento.Abrir(documento.CaminhoRelativo));
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            throw new DominioException(
                CodigosErro.DocumentoIndisponivel,
                "Não foi possível obter o documento; os restantes dados da ficha não foram afetados. Tente novamente.");
        }
    }

    /// <summary>
    /// Obtém a receita mais recente de cada tratamento de uma ficha (RF-052). Chamado depois de verificada a associação à ficha.
    /// </summary>
    /// <param name="fichaId">Ficha dos tratamentos.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O identificador da receita mais recente, por tratamento.</returns>
    public async Task<IReadOnlyDictionary<Guid, Guid>> ReceitasPorTratamentoAsync(Guid fichaId, CancellationToken cancelamento)
    {
        var documentos = await this.documentos.DaFichaAsync(fichaId, cancelamento).ConfigureAwait(false);
        return documentos
            .Where(d => d.EReceita() && d.TratamentoId is not null)
            .OrderByDescending(d => d.CarregadoEmUtc)
            .GroupBy(d => d.TratamentoId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);
    }

    /// <summary>
    /// Remove do armazenamento os ficheiros de uma ficha eliminada, depois de a transação ser confirmada (RF-123, RNF-046).
    /// </summary>
    /// <param name="caminhosRelativos">Caminhos dos ficheiros a remover.</param>
    public void EliminarFicheiros(IEnumerable<string> caminhosRelativos)
    {
        ArgumentNullException.ThrowIfNull(caminhosRelativos);

        foreach (var caminho in caminhosRelativos)
        {
            this.armazenamento.Eliminar(caminho);
        }
    }

    private static CategoriaDocumento? LerCategoria(string? valor)
    {
        var limpo = ValidadorDados.Normalizar(valor);
        return limpo is not null && Enum.GetNames<CategoriaDocumento>().Contains(limpo, StringComparer.Ordinal)
            ? Enum.Parse<CategoriaDocumento>(limpo)
            : null;
    }

    private static string Campo(string prefixo, string campo)
    {
        return prefixo.Length == 0 ? campo : $"{prefixo}.{campo}";
    }

    private static string NomeOriginal(string? nome, FormatoDocumento formato)
    {
        var limpo = ValidadorDados.Normalizar(Path.GetFileName(nome ?? string.Empty));
        if (limpo is null)
        {
            return $"documento.{formato.ToString().ToLowerInvariant()}";
        }

        return limpo.Length <= ComprimentoMaximoNome ? limpo : limpo[^ComprimentoMaximoNome..];
    }

    private static async Task<FormatoDocumento?> DetetarFormatoAsync(Func<Stream> abrir, CancellationToken cancelamento)
    {
        var cabecalho = new byte[AssinaturaPng.Length];
        int lidos;
        var conteudo = abrir();
        await using (conteudo.ConfigureAwait(false))
        {
            lidos = await conteudo.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, cancelamento).ConfigureAwait(false);
        }

        return FormatoDoCabecalho(cabecalho.AsSpan(0, lidos));
    }

    private static FormatoDocumento? FormatoDoCabecalho(ReadOnlySpan<byte> inicio)
    {
        if (inicio.StartsWith(AssinaturaPdf))
        {
            return FormatoDocumento.PDF;
        }

        if (inicio.StartsWith(AssinaturaPng))
        {
            return FormatoDocumento.PNG;
        }

        return inicio.StartsWith(AssinaturaJpg) ? FormatoDocumento.JPG : null;
    }
}
