// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Tomas;
using DoseCerta.Api.Erros;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Horários de toma e confirmação de toma (F006). Endpoints: OP-32, OP-33 e OP-34.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class TomasController : ControllerBase
{
    private readonly TomaService tomaService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomasController"/> class.
    /// </summary>
    /// <param name="tomaService">Serviço das tomas.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar as datas com o desvio de Lisboa.</param>
    public TomasController(TomaService tomaService, IRelogio relogio)
    {
        this.tomaService = tomaService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-32 Listar tomas da ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="de">Primeiro dia (AAAA-MM-DD), em hora de Lisboa; obrigatório.</param>
    /// <param name="ate">Último dia (AAAA-MM-DD), em hora de Lisboa; obrigatório, no máximo 31 dias contando os dois.</param>
    /// <param name="estado">Filtro opcional pelo estado da toma.</param>
    /// <param name="tratamentoId">Filtro opcional pelo tratamento.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com as tomas, por ordem do instante previsto (RF-063, RF-065).</returns>
    [HttpGet("fichas/{fichaId:guid}/tomas")]
    [ProducesResponseType(typeof(IReadOnlyList<RespostaToma>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarTomasDaFicha(
        Guid fichaId,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] EstadoToma? estado,
        [FromQuery] Guid? tratamentoId,
        CancellationToken cancelamento)
    {
        if (estado is not null && !Enum.IsDefined(estado.Value))
        {
            throw ValidacaoException.DoCampo("estado", "Tem de ser Pendente, Confirmada, ConfirmadaComAtraso ou NaoConfirmada.");
        }

        var lista = await this.tomaService
            .ListarAsync(UtilizadorAutenticado.Id(this.User), fichaId, de, ate, estado, tratamentoId, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(lista.Select(this.Resposta).ToList());
    }

    /// <summary>
    /// OP-33 Confirmar toma, sem corpo. Só um Cuidador confirma; a primeira confirmação mantém-se.
    /// </summary>
    /// <param name="tomaId">Identificador da toma.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com a toma confirmada, com quem confirmou e quando (RF-064 a RF-067).</returns>
    [HttpPost("tomas/{tomaId:guid}/confirmacao")]
    [ProducesResponseType(typeof(RespostaToma), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmarToma(Guid tomaId, CancellationToken cancelamento)
    {
        var toma = await this.tomaService
            .ConfirmarAsync(UtilizadorAutenticado.Id(this.User), tomaId, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(this.Resposta(toma));
    }

    /// <summary>
    /// OP-34 Sincronizar o calendário de tomas: tomas pendentes das próximas 48 horas das fichas ativas em que é Cuidador.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com as tomas a sincronizar; lista vazia se não houver (RF-061, RF-062, RF-069).</returns>
    [HttpGet("calendario/tomas")]
    [ProducesResponseType(typeof(IReadOnlyList<RespostaTomaSincronizada>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SincronizarOCalendarioDeTomas(CancellationToken cancelamento)
    {
        var lista = await this.tomaService
            .CalendarioParaSincronizarAsync(UtilizadorAutenticado.Id(this.User), cancelamento)
            .ConfigureAwait(false);
        var calendario = this.relogio.Calendario();
        return this.Ok(lista
            .Select(t => new RespostaTomaSincronizada(
                t.TomaId,
                t.FichaId,
                t.NomeFicha,
                t.Medicamento,
                t.Dose,
                calendario.ParaLocal(t.PrevistaEmUtc)))
            .ToList());
    }

    private RespostaToma Resposta(TomaDaFicha dados)
    {
        var calendario = this.relogio.Calendario();
        var t = dados.Toma;
        return new RespostaToma(
            t.Id,
            t.TratamentoId,
            t.Medicamento,
            t.Dose,
            t.GrauImportancia,
            calendario.ParaLocal(t.PrevistaEmUtc),
            t.Estado,
            t.ConfirmadaPorId is Guid porId ? new PessoaConfirmou(porId, dados.ConfirmadaPorNome ?? string.Empty) : null,
            t.ConfirmadaEmUtc is DateTime emUtc ? calendario.ParaLocal(emUtc) : null);
    }
}
