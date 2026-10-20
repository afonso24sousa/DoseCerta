// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Fichas;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Documentos;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Documentos das fichas; é a única forma de obter um ficheiro, sempre com verificação da associação à ficha (F002, RNF-007).
/// Endpoints: OP-15, OP-16 e OP-17.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class DocumentosController : ControllerBase
{
    private readonly DocumentoService documentoService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentosController"/> class.
    /// </summary>
    /// <param name="documentoService">Serviço dos documentos.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar os instantes com o desvio de Lisboa.</param>
    public DocumentosController(DocumentoService documentoService, IRelogio relogio)
    {
        this.documentoService = documentoService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-15 Carregar documento na ficha (<c>multipart/form-data</c> com as partes <c>categoria</c> e <c>ficheiro</c>).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="categoria">Categoria: <c>ReceitaMedica</c>, <c>RelatorioClinico</c> ou <c>DocumentoIdentificacao</c>.</param>
    /// <param name="ficheiro">Ficheiro PDF, JPG ou PNG até 10 MB.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com os metadados do documento.</returns>
    [HttpPost("fichas/{fichaId:guid}/documentos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CarregarDocumentoNaFicha(
        Guid fichaId,
        [FromForm] string? categoria,
        IFormFile? ficheiro,
        CancellationToken cancelamento)
    {
        var submetido = new DocumentoSubmetido(categoria, ficheiro?.FileName, ficheiro?.Length ?? 0, ficheiro is null ? null : ficheiro.OpenReadStream);
        var documento = await this.documentoService
            .CarregarAsync(UtilizadorAutenticado.Id(this.User), fichaId, submetido, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, this.Resposta(documento));
    }

    /// <summary>
    /// OP-16 Listar documentos da ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os metadados e a categoria de cada documento.</returns>
    [HttpGet("fichas/{fichaId:guid}/documentos")]
    public async Task<IActionResult> ListarDocumentosDaFicha(Guid fichaId, CancellationToken cancelamento)
    {
        var documentos = await this.documentoService
            .ListarAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(documentos.Select(this.Resposta).ToList());
    }

    /// <summary>
    /// OP-17 Descarregar documento.
    /// </summary>
    /// <param name="documentoId">Identificador do documento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o conteúdo do ficheiro; 503 se o ficheiro não puder ser obtido (RF-124).</returns>
    [HttpGet("documentos/{documentoId:guid}")]
    public async Task<IActionResult> DescarregarDocumento(Guid documentoId, CancellationToken cancelamento)
    {
        var lido = await this.documentoService
            .ObterAsync(UtilizadorAutenticado.Id(this.User), documentoId, cancelamento)
            .ConfigureAwait(false);
        return this.File(lido.Conteudo, TipoConteudo(lido.Documento.Formato), lido.Documento.NomeOriginal);
    }

    /// <summary>
    /// Obtém o tipo de conteúdo HTTP de um formato de documento.
    /// </summary>
    /// <param name="formato">Formato do documento.</param>
    /// <returns>O tipo de conteúdo, por exemplo <c>application/pdf</c>.</returns>
    internal static string TipoConteudo(FormatoDocumento formato)
    {
        return formato switch
        {
            FormatoDocumento.PDF => "application/pdf",
            FormatoDocumento.JPG => "image/jpeg",
            _ => "image/png",
        };
    }

    private RespostaDocumento Resposta(Documento documento)
    {
        return new RespostaDocumento(
            documento.Id,
            documento.Categoria,
            documento.NomeOriginal,
            documento.Formato,
            documento.TamanhoBytes,
            this.relogio.Calendario().ParaLocal(documento.CarregadoEmUtc));
    }
}
