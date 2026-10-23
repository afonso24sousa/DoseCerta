// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Convites;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Convites;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Convites para uma ficha: criar, listar os recebidos, aceitar e recusar (F004).
/// Endpoints: OP-19, OP-20, OP-21 e OP-22.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class ConvitesController : ControllerBase
{
    private readonly ConviteService conviteService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvitesController"/> class.
    /// </summary>
    /// <param name="conviteService">Serviço dos convites.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar os instantes com o desvio de Lisboa.</param>
    public ConvitesController(ConviteService conviteService, IRelogio relogio)
    {
        this.conviteService = conviteService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-19 Convidar pessoa para a ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">E-mail, papel oferecido e indicação de convite para Cuidador principal.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com o convite pendente.</returns>
    [HttpPost("fichas/{fichaId:guid}/convites")]
    public async Task<IActionResult> ConvidarPessoaParaAFicha(Guid fichaId, [FromBody] PedidoConvite pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var convite = await this.conviteService
            .ConvidarAsync(UtilizadorAutenticado.Id(this.User), fichaId, pedido.Email, pedido.Papel, pedido.ParaPrincipal ?? false, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, this.Resposta(convite));
    }

    /// <summary>
    /// OP-20 Listar convites recebidos.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os convites pendentes dirigidos ao e-mail do Utilizador.</returns>
    [HttpGet("convites")]
    public async Task<IActionResult> ListarConvitesRecebidos(CancellationToken cancelamento)
    {
        var convites = await this.conviteService.ListarRecebidosAsync(UtilizadorAutenticado.Id(this.User), cancelamento).ConfigureAwait(false);
        return this.Ok(convites.Select(this.Resposta).ToList());
    }

    /// <summary>
    /// OP-21 Aceitar convite.
    /// </summary>
    /// <param name="conviteId">Identificador do convite.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com a pessoa associada à ficha e o seu papel.</returns>
    [HttpPost("convites/{conviteId:guid}/aceitacao")]
    public async Task<IActionResult> AceitarConvite(Guid conviteId, CancellationToken cancelamento)
    {
        var pessoa = await this.conviteService.AceitarAsync(UtilizadorAutenticado.Id(this.User), conviteId, cancelamento).ConfigureAwait(false);
        return this.Ok(PessoasController.Resposta(pessoa));
    }

    /// <summary>
    /// OP-22 Recusar convite.
    /// </summary>
    /// <param name="conviteId">Identificador do convite.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo.</returns>
    [HttpPost("convites/{conviteId:guid}/recusa")]
    public async Task<IActionResult> RecusarConvite(Guid conviteId, CancellationToken cancelamento)
    {
        await this.conviteService.RecusarAsync(UtilizadorAutenticado.Id(this.User), conviteId, cancelamento).ConfigureAwait(false);
        return this.NoContent();
    }

    private RespostaConvite Resposta(ConviteResumo convite)
    {
        return new RespostaConvite(
            convite.Id,
            convite.FichaId,
            convite.NomeFicha,
            convite.PapelOferecido,
            convite.ParaPrincipal,
            convite.ConvidadoPor,
            convite.Estado,
            this.relogio.Calendario().ParaLocal(convite.CriadoEmUtc));
    }
}
