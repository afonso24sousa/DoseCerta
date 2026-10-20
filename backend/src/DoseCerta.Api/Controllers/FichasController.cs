// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json;
using DoseCerta.Api.Contratos.Fichas;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Fichas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Fichas de utentes: listar, criar, consultar, editar, arquivar, reativar e eliminar (F002, F003).
/// Endpoints: OP-07, OP-09, OP-10, OP-11, OP-12, OP-13 e OP-14.
/// </summary>
/// <remarks>
/// A criação e a edição aceitam <c>application/json</c> ou, com documentos, <c>multipart/form-data</c>: a parte <c>dados</c> leva o JSON
/// do pedido, cada parte <c>documento</c> leva um ficheiro e as partes <c>categoria</c> levam, pela mesma ordem, a categoria de cada documento.
/// </remarks>
[ApiController]
[Route("api/v1/fichas")]
public sealed class FichasController : ControllerBase
{
    /// <summary>
    /// Nome da parte com o JSON do pedido, em <c>multipart/form-data</c>.
    /// </summary>
    public const string ParteDados = "dados";

    /// <summary>
    /// Nome das partes com os ficheiros dos documentos, em <c>multipart/form-data</c>.
    /// </summary>
    public const string ParteDocumento = "documento";

    /// <summary>
    /// Nome das partes com a categoria de cada documento, pela ordem dos ficheiros.
    /// </summary>
    public const string ParteCategoria = "categoria";

    private readonly FichaService fichaService;
    private readonly JsonSerializerOptions opcoesJson;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichasController"/> class.
    /// </summary>
    /// <param name="fichaService">Serviço das fichas.</param>
    /// <param name="opcoesJson">Opções JSON da API, usadas para ler a parte <c>dados</c>.</param>
    public FichasController(FichaService fichaService, IOptions<JsonOptions> opcoesJson)
    {
        ArgumentNullException.ThrowIfNull(opcoesJson);
        this.fichaService = fichaService;
        this.opcoesJson = opcoesJson.Value.JsonSerializerOptions;
    }

    /// <summary>
    /// OP-07 Listar fichas do Utilizador.
    /// </summary>
    /// <param name="estado">Filtro opcional: <c>Ativa</c> ou <c>Arquivada</c>; sem ele, devolve as duas.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com as fichas a que o Utilizador está associado (RNF-005).</returns>
    [HttpGet]
    public async Task<IActionResult> ListarFichasDoUtilizador([FromQuery] EstadoFicha? estado, CancellationToken cancelamento)
    {
        if (estado is not null && !Enum.IsDefined(estado.Value))
        {
            throw ValidacaoException.DoCampo("estado", "Tem de ser Ativa ou Arquivada.");
        }

        var fichas = await this.fichaService.ListarAsync(UtilizadorAutenticado.Id(this.User), estado, cancelamento).ConfigureAwait(false);
        return this.Ok(fichas.Select(Resumo).ToList());
    }

    /// <summary>
    /// OP-09 Criar ficha (de terceiro ou própria), sem documentos.
    /// </summary>
    /// <param name="pedido">Dados da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com a ficha criada.</returns>
    [HttpPost]
    [Consumes("application/json")]
    public Task<IActionResult> CriarFicha([FromBody] PedidoCriacaoFicha pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return this.CriarAsync(pedido, Array.Empty<DocumentoSubmetido>(), cancelamento);
    }

    /// <summary>
    /// OP-09 Criar ficha (de terceiro ou própria), com documentos; a criação é atómica (RF-023, RNF-046).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com a ficha criada.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CriarFichaComDocumentos(CancellationToken cancelamento)
    {
        var formulario = await this.Request.ReadFormAsync(cancelamento).ConfigureAwait(false);
        var pedido = this.LerDados<PedidoCriacaoFicha>(formulario);
        return await this.CriarAsync(pedido, Documentos(formulario), cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-10 Consultar ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os dados de identificação e as necessidades; 404 a quem não está associado (RNF-005).</returns>
    [HttpGet("{fichaId:guid}")]
    public async Task<IActionResult> ConsultarFicha(Guid fichaId, CancellationToken cancelamento)
    {
        var acesso = await this.fichaService.ObterAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento).ConfigureAwait(false);
        return this.Ok(Resposta(acesso));
    }

    /// <summary>
    /// OP-11 Editar ficha, sem documentos.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">Dados da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com a ficha atualizada.</returns>
    [HttpPut("{fichaId:guid}")]
    [Consumes("application/json")]
    public Task<IActionResult> EditarFicha(Guid fichaId, [FromBody] PedidoEdicaoFicha pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return this.EditarAsync(fichaId, pedido, Array.Empty<DocumentoSubmetido>(), cancelamento);
    }

    /// <summary>
    /// OP-11 Editar ficha, acrescentando documentos; se um documento não for guardado, nada é alterado (RF-023, RNF-046).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com a ficha atualizada.</returns>
    [HttpPut("{fichaId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> EditarFichaComDocumentos(Guid fichaId, CancellationToken cancelamento)
    {
        var formulario = await this.Request.ReadFormAsync(cancelamento).ConfigureAwait(false);
        var pedido = this.LerDados<PedidoEdicaoFicha>(formulario);
        return await this.EditarAsync(fichaId, pedido, Documentos(formulario), cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-12 Arquivar ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo.</returns>
    [HttpPost("{fichaId:guid}/arquivo")]
    public async Task<IActionResult> ArquivarFicha(Guid fichaId, CancellationToken cancelamento)
    {
        await this.fichaService.ArquivarAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>
    /// OP-13 Reativar ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo.</returns>
    [HttpDelete("{fichaId:guid}/arquivo")]
    public async Task<IActionResult> ReativarFicha(Guid fichaId, CancellationToken cancelamento)
    {
        await this.fichaService.ReativarAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>
    /// OP-14 Eliminar ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo; 409 se a ficha tiver medicação, tarefas ou histórico (RF-123).</returns>
    [HttpDelete("{fichaId:guid}")]
    public async Task<IActionResult> EliminarFicha(Guid fichaId, CancellationToken cancelamento)
    {
        await this.fichaService.EliminarAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>
    /// Converte uma ficha e a associação do Utilizador na resposta da API.
    /// </summary>
    /// <param name="acesso">Ficha e associação.</param>
    /// <returns>A resposta com os dados da ficha.</returns>
    internal static RespostaFicha Resposta(AcessoFicha acesso)
    {
        var ficha = acesso.Ficha;
        return new RespostaFicha(
            ficha.Id,
            ficha.NomeCompleto,
            ficha.DataNascimento,
            ficha.ContactoEmergencia,
            ficha.EProprio(),
            ficha.Estado,
            ficha.GrauAutonomia is null ? null : new RespostaItemDominio(ficha.GrauAutonomia.Id, ficha.GrauAutonomia.Designacao),
            ficha.CondicoesSaude.OrderBy(c => c.Id).Select(c => new RespostaItemDominio(c.Id, c.Designacao)).ToList(),
            ficha.Alergias.OrderBy(a => a.Id).Select(a => new RespostaItemDominio(a.Id, a.Designacao)).ToList(),
            acesso.Associacao.Papel,
            acesso.Associacao.EPrincipal);
    }

    private static RespostaResumoFicha Resumo(AcessoFicha acesso)
    {
        var ficha = acesso.Ficha;
        return new RespostaResumoFicha(
            ficha.Id,
            ficha.NomeCompleto,
            ficha.EProprio(),
            ficha.Estado,
            acesso.Associacao.Papel,
            acesso.Associacao.EPrincipal);
    }

    private static List<DocumentoSubmetido> Documentos(IFormCollection formulario)
    {
        var categorias = formulario[ParteCategoria];
        return formulario.Files.GetFiles(ParteDocumento)
            .Select((ficheiro, i) => new DocumentoSubmetido(
                i < categorias.Count ? categorias[i] : null,
                ficheiro.FileName,
                ficheiro.Length,
                ficheiro.OpenReadStream))
            .ToList();
    }

    private T LerDados<T>(IFormCollection formulario)
        where T : class
    {
        var json = formulario[ParteDados].ToString();
        const string Regra = "É obrigatória e tem de conter os dados do pedido em JSON válido.";
        if (string.IsNullOrWhiteSpace(json))
        {
            throw ValidacaoException.DoCampo(ParteDados, Regra);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, this.opcoesJson) ?? throw ValidacaoException.DoCampo(ParteDados, Regra);
        }
        catch (JsonException)
        {
            throw ValidacaoException.DoCampo(ParteDados, Regra);
        }
    }

    private async Task<IActionResult> CriarAsync(PedidoCriacaoFicha pedido, IReadOnlyList<DocumentoSubmetido> documentos, CancellationToken cancelamento)
    {
        var principal = pedido.CuidadorPrincipal is null
            ? null
            : new DefinicaoPrincipal(pedido.CuidadorPrincipal.Eu, pedido.CuidadorPrincipal.Email);
        var dados = new DadosFicha(
            pedido.NomeCompleto,
            pedido.DataNascimento,
            pedido.ContactoEmergencia,
            pedido.GrauAutonomiaId,
            pedido.CondicoesSaudeIds,
            pedido.AlergiasIds);

        var acesso = await this.fichaService
            .CriarAsync(UtilizadorAutenticado.Id(this.User), dados, pedido.Propria ?? false, principal, documentos, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, Resposta(acesso));
    }

    private async Task<IActionResult> EditarAsync(Guid fichaId, PedidoEdicaoFicha pedido, IReadOnlyList<DocumentoSubmetido> documentos, CancellationToken cancelamento)
    {
        var dados = new DadosFicha(
            pedido.NomeCompleto,
            pedido.DataNascimento,
            pedido.ContactoEmergencia,
            pedido.GrauAutonomiaId,
            pedido.CondicoesSaudeIds,
            pedido.AlergiasIds);

        var acesso = await this.fichaService
            .EditarAsync(UtilizadorAutenticado.Id(this.User), fichaId, dados, documentos, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(acesso));
    }
}
