// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Agenda;
using DoseCerta.Api.Erros;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Agenda em vista diária, semanal e mensal (F008). Endpoint: OP-45.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class AgendaController : ControllerBase
{
    private readonly AgendaService agendaService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgendaController"/> class.
    /// </summary>
    /// <param name="agendaService">Serviço da agenda.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar as datas com o desvio de Lisboa.</param>
    public AgendaController(AgendaService agendaService, IRelogio relogio)
    {
        this.agendaService = agendaService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-45 Consultar agenda.
    /// </summary>
    /// <param name="vista">Vista: <c>dia</c>, <c>semana</c> (segunda a domingo) ou <c>mes</c>; obrigatória.</param>
    /// <param name="data">Data (AAAA-MM-DD), em hora de Lisboa, que define o intervalo; obrigatória.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os compromissos e as tomas das fichas ativas do Utilizador, por ordem do início (RF-083 a RF-089).</returns>
    [HttpGet("agenda")]
    [ProducesResponseType(typeof(IReadOnlyList<RespostaItemAgenda>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConsultarAgenda([FromQuery] string? vista, [FromQuery] DateOnly? data, CancellationToken cancelamento)
    {
        var itens = await this.agendaService
            .ConsultarAsync(UtilizadorAutenticado.Id(this.User), vista, data, cancelamento)
            .ConfigureAwait(false);
        var calendario = this.relogio.Calendario();
        return this.Ok(itens
            .Select(i => new RespostaItemAgenda(
                i.Id,
                i.Tipo,
                i.FichaId,
                i.NomeFicha,
                i.Titulo,
                calendario.ParaLocal(i.InicioUtc),
                i.Estado,
                i.Confirmavel))
            .ToList());
    }
}
