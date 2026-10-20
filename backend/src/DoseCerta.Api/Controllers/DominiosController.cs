// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Fichas;
using DoseCerta.Application.Fichas;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Listas fechadas de condições de saúde, alergias e graus de autonomia (F002). Endpoint: OP-08.
/// </summary>
[ApiController]
[Route("api/v1/dominios")]
public sealed class DominiosController : ControllerBase
{
    private readonly FichaService fichaService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DominiosController"/> class.
    /// </summary>
    /// <param name="fichaService">Serviço das fichas.</param>
    public DominiosController(FichaService fichaService)
    {
        this.fichaService = fichaService;
    }

    /// <summary>
    /// OP-08 Obter as listas de necessidades.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com as três listas fechadas (RF-021).</returns>
    [HttpGet("necessidades")]
    public async Task<IActionResult> ObterAsListasDeNecessidades(CancellationToken cancelamento)
    {
        var dominios = await this.fichaService.ObterDominiosAsync(cancelamento).ConfigureAwait(false);
        return this.Ok(new RespostaDominios(
            dominios.CondicoesSaude.Select(c => new RespostaItemDominio(c.Id, c.Designacao)).ToList(),
            dominios.Alergias.Select(a => new RespostaItemDominio(a.Id, a.Designacao)).ToList(),
            dominios.GrausAutonomia.Select(g => new RespostaItemDominio(g.Id, g.Designacao)).ToList()));
    }
}
