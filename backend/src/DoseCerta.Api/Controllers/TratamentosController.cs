// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Text.Json;
using DoseCerta.Api.Contratos.Tratamentos;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Documentos;
using DoseCerta.Application.Tratamentos;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Tratamentos (F005). Endpoints: OP-27, OP-28, OP-29, OP-30 e OP-31.
/// </summary>
/// <remarks>
/// O registo e a atualização aceitam <c>application/json</c> ou, com receita, <c>multipart/form-data</c>: a parte
/// <c>dados</c> leva o JSON do pedido e a parte <c>receita</c> leva o ficheiro (PDF, JPG ou PNG até 10 MB).
/// </remarks>
[ApiController]
[Route("api/v1")]
public sealed class TratamentosController : ControllerBase
{
    /// <summary>
    /// Nome da parte com o ficheiro da receita, em <c>multipart/form-data</c>.
    /// </summary>
    public const string ParteReceita = "receita";

    private readonly TratamentoService tratamentoService;
    private readonly JsonSerializerOptions opcoesJson;

    /// <summary>
    /// Initializes a new instance of the <see cref="TratamentosController"/> class.
    /// </summary>
    /// <param name="tratamentoService">Serviço dos tratamentos.</param>
    /// <param name="opcoesJson">Opções JSON da API, usadas para ler a parte <c>dados</c>.</param>
    public TratamentosController(TratamentoService tratamentoService, IOptions<JsonOptions> opcoesJson)
    {
        ArgumentNullException.ThrowIfNull(opcoesJson);
        this.tratamentoService = tratamentoService;
        this.opcoesJson = opcoesJson.Value.JsonSerializerOptions;
    }

    /// <summary>
    /// OP-27 Registar tratamento, sem receita.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">Dados do tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com o tratamento registado; as tomas previstas ficam geradas (RF-060).</returns>
    [HttpPost("fichas/{fichaId:guid}/tratamentos")]
    [Consumes("application/json")]
    public Task<IActionResult> RegistarTratamento(Guid fichaId, [FromBody] PedidoTratamento pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return this.RegistarAsync(fichaId, pedido, null, cancelamento);
    }

    /// <summary>
    /// OP-27 Registar tratamento, com receita; se a receita não for guardada, nada é gravado (RF-053, RNF-046).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com o tratamento registado.</returns>
    [HttpPost("fichas/{fichaId:guid}/tratamentos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RegistarTratamentoComReceita(Guid fichaId, CancellationToken cancelamento)
    {
        var formulario = await this.Request.ReadFormAsync(cancelamento).ConfigureAwait(false);
        var pedido = LeitorMultipart.LerDados<PedidoTratamento>(formulario, this.opcoesJson);
        return await this.RegistarAsync(fichaId, pedido, Receita(formulario), cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-28 Listar tratamentos da ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="estado">Filtro opcional: <c>EmCurso</c> ou <c>Terminado</c>; sem ele, devolve os dois.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os tratamentos, identificados pelo estado (RF-054).</returns>
    [HttpGet("fichas/{fichaId:guid}/tratamentos")]
    public async Task<IActionResult> ListarTratamentosDaFicha(Guid fichaId, [FromQuery] EstadoTratamento? estado, CancellationToken cancelamento)
    {
        if (estado is not null && !Enum.IsDefined(estado.Value))
        {
            throw ValidacaoException.DoCampo("estado", "Tem de ser EmCurso ou Terminado.");
        }

        var lista = await this.tratamentoService
            .ListarAsync(UtilizadorAutenticado.Id(this.User), fichaId, estado, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(lista.Select(Resposta).ToList());
    }

    /// <summary>
    /// OP-29 Consultar tratamento.
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o tratamento; 404 a quem não está associado à ficha (RNF-005).</returns>
    [HttpGet("tratamentos/{tratamentoId:guid}")]
    public async Task<IActionResult> ConsultarTratamento(Guid tratamentoId, CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentoService
            .ObterAsync(UtilizadorAutenticado.Id(this.User), tratamentoId, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(tratamento));
    }

    /// <summary>
    /// OP-30 Atualizar tratamento, sem receita nova.
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="pedido">Dados a alterar.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o tratamento alterado.</returns>
    [HttpPut("tratamentos/{tratamentoId:guid}")]
    [Consumes("application/json")]
    public Task<IActionResult> AtualizarTratamento(Guid tratamentoId, [FromBody] PedidoTratamento pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return this.AtualizarAsync(tratamentoId, pedido, null, cancelamento);
    }

    /// <summary>
    /// OP-30 Atualizar tratamento, com receita nova; se a receita não for guardada, o tratamento fica como estava (RF-053).
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o tratamento alterado.</returns>
    [HttpPut("tratamentos/{tratamentoId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AtualizarTratamentoComReceita(Guid tratamentoId, CancellationToken cancelamento)
    {
        var formulario = await this.Request.ReadFormAsync(cancelamento).ConfigureAwait(false);
        var pedido = LeitorMultipart.LerDados<PedidoTratamento>(formulario, this.opcoesJson);
        return await this.AtualizarAsync(tratamentoId, pedido, Receita(formulario), cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// OP-31 Terminar tratamento antecipadamente.
    /// </summary>
    /// <param name="tratamentoId">Identificador do tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o tratamento terminado; as tomas previstas posteriores deixam de existir (RF-056, RF-060).</returns>
    [HttpPost("tratamentos/{tratamentoId:guid}/termino")]
    public async Task<IActionResult> TerminarTratamentoAntecipadamente(Guid tratamentoId, CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentoService
            .TerminarAsync(UtilizadorAutenticado.Id(this.User), tratamentoId, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(tratamento));
    }

    /// <summary>
    /// Converte um tratamento na resposta da API.
    /// </summary>
    /// <param name="dados">Tratamento e receita.</param>
    /// <returns>A resposta com os dados do tratamento.</returns>
    internal static RespostaTratamento Resposta(TratamentoComReceita dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var t = dados.Tratamento;
        return new RespostaTratamento(
            t.Id,
            t.FichaId,
            t.Medicamento,
            t.Dose,
            t.GrauImportancia,
            t.DataInicio,
            t.DataFim,
            t.Horarios.Select(h => h.HoraLocal).Order().Select(h => h.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList(),
            t.Estado,
            dados.ReceitaDocumentoId);
    }

    private static DocumentoSubmetido? Receita(IFormCollection formulario)
    {
        var ficheiro = formulario.Files.GetFile(ParteReceita);
        return ficheiro is null ? null : new DocumentoSubmetido(null, ficheiro.FileName, ficheiro.Length, ficheiro.OpenReadStream);
    }

    private static DadosTratamento Dados(PedidoTratamento pedido)
    {
        return new DadosTratamento(pedido.Medicamento, pedido.Dose, pedido.GrauImportancia, pedido.DataInicio, pedido.DataFim, pedido.Horarios);
    }

    private async Task<IActionResult> RegistarAsync(Guid fichaId, PedidoTratamento pedido, DocumentoSubmetido? receita, CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentoService
            .RegistarAsync(UtilizadorAutenticado.Id(this.User), fichaId, Dados(pedido), receita, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, Resposta(tratamento));
    }

    private async Task<IActionResult> AtualizarAsync(Guid tratamentoId, PedidoTratamento pedido, DocumentoSubmetido? receita, CancellationToken cancelamento)
    {
        var tratamento = await this.tratamentoService
            .AtualizarAsync(UtilizadorAutenticado.Id(this.User), tratamentoId, Dados(pedido), receita, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(tratamento));
    }
}
