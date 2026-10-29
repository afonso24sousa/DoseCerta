// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Compromissos;
using DoseCerta.Api.Erros;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Compromissos;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Compromissos de saúde (F008). Endpoints: OP-41, OP-42, OP-43 e OP-44. Não há operação de confirmação: o pedido de
/// confirmação de um compromisso é recusado com 405 (RF-087).
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class CompromissosController : ControllerBase
{
    private readonly CompromissoService compromissoService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompromissosController"/> class.
    /// </summary>
    /// <param name="compromissoService">Serviço dos compromissos.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar as datas com o desvio de Lisboa.</param>
    public CompromissosController(CompromissoService compromissoService, IRelogio relogio)
    {
        this.compromissoService = compromissoService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-41 Registar compromisso.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">Dados do compromisso.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com o compromisso registado (RF-080, RF-116).</returns>
    [HttpPost("fichas/{fichaId:guid}/compromissos")]
    [ProducesResponseType(typeof(RespostaCompromisso), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistarCompromisso(Guid fichaId, [FromBody] PedidoCompromisso pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var compromisso = await this.compromissoService
            .RegistarAsync(UtilizadorAutenticado.Id(this.User), fichaId, Dados(pedido), cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, this.Resposta(compromisso));
    }

    /// <summary>
    /// OP-42 Listar compromissos da ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="de">Primeiro dia (AAAA-MM-DD), em hora de Lisboa, inclusive; opcional.</param>
    /// <param name="ate">Último dia (AAAA-MM-DD), em hora de Lisboa, inclusive; opcional.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os compromissos, por ordem da data e hora.</returns>
    [HttpGet("fichas/{fichaId:guid}/compromissos")]
    [ProducesResponseType(typeof(IReadOnlyList<RespostaCompromisso>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarCompromissosDaFicha(
        Guid fichaId,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        CancellationToken cancelamento)
    {
        var lista = await this.compromissoService
            .ListarAsync(UtilizadorAutenticado.Id(this.User), fichaId, de, ate, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(lista.Select(this.Resposta).ToList());
    }

    /// <summary>
    /// OP-43 Editar compromisso.
    /// </summary>
    /// <param name="compromissoId">Identificador do compromisso.</param>
    /// <param name="pedido">Dados novos.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o compromisso editado (RF-081, RF-117).</returns>
    [HttpPut("compromissos/{compromissoId:guid}")]
    [ProducesResponseType(typeof(RespostaCompromisso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarCompromisso(Guid compromissoId, [FromBody] PedidoCompromisso pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var compromisso = await this.compromissoService
            .EditarAsync(UtilizadorAutenticado.Id(this.User), compromissoId, Dados(pedido), cancelamento)
            .ConfigureAwait(false);
        return this.Ok(this.Resposta(compromisso));
    }

    /// <summary>
    /// OP-44 Remover compromisso.
    /// </summary>
    /// <param name="compromissoId">Identificador do compromisso.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204; o compromisso deixa de aparecer na agenda e o aviso deixa de ser emitido (RF-082, RF-117).</returns>
    [HttpDelete("compromissos/{compromissoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoverCompromisso(Guid compromissoId, CancellationToken cancelamento)
    {
        await this.compromissoService
            .RemoverAsync(UtilizadorAutenticado.Id(this.User), compromissoId, cancelamento)
            .ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>
    /// Pedido de confirmação de um compromisso: não é uma operação da API e é sempre recusado, para qualquer papel (RF-087).
    /// </summary>
    /// <param name="compromissoId">Identificador do compromisso.</param>
    /// <returns>Nunca devolve: responde 405 <c>COMPROMISSO_NAO_CONFIRMAVEL</c>.</returns>
    /// <exception cref="DominioException">Sempre, com <see cref="CodigosErro.CompromissoNaoConfirmavel"/>.</exception>
    [HttpPost("compromissos/{compromissoId:guid}/confirmacao")]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status405MethodNotAllowed)]
    public IActionResult ConfirmarCompromisso(Guid compromissoId)
    {
        throw new DominioException(
            CodigosErro.CompromissoNaoConfirmavel,
            "Os compromissos não são confirmáveis: ficam na agenda apenas para consulta.");
    }

    private static DadosCompromisso Dados(PedidoCompromisso pedido)
    {
        return new DadosCompromisso(pedido.DataHora, pedido.Local, pedido.Motivo, pedido.AntecedenciaAvisoMinutos);
    }

    private RespostaCompromisso Resposta(Compromisso compromisso)
    {
        return new RespostaCompromisso(
            compromisso.Id,
            compromisso.FichaId,
            this.relogio.Calendario().ParaLocal(compromisso.DataHoraUtc),
            compromisso.Local,
            compromisso.Motivo,
            compromisso.AntecedenciaAvisoMin);
    }
}
