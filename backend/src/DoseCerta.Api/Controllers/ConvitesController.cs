// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Convites;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Convites;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Convites para uma ficha: criar, listar os recebidos, aceitar e recusar (F004).
/// Endpoints: OP-19.
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
