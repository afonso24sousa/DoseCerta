// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Notificacoes;
using DoseCerta.Api.Erros;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Notificacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Histórico de notificações (F011). Endpoint: OP-51.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class NotificacoesController : ControllerBase
{
    private readonly NotificacaoService notificacaoService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificacoesController"/> class.
    /// </summary>
    /// <param name="notificacaoService">Serviço das notificações.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar as datas com o desvio de Lisboa.</param>
    public NotificacoesController(NotificacaoService notificacaoService, IRelogio relogio)
    {
        this.notificacaoService = notificacaoService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-51 Consultar histórico de notificações: as notificações recebidas pelo Utilizador autenticado, da mais recente para a
    /// mais antiga, com o estado do e-mail. Qualquer Utilizador autenticado consulta o seu histórico.
    /// </summary>
    /// <param name="de">Primeiro dia (AAAA-MM-DD), em hora de Lisboa; opcional.</param>
    /// <param name="ate">Último dia (AAAA-MM-DD), em hora de Lisboa, inclusive; opcional e não anterior a <paramref name="de"/>.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com as notificações; lista vazia se não houver (RF-119, RF-140, RF-141).</returns>
    [HttpGet("notificacoes")]
    [ProducesResponseType(typeof(IReadOnlyList<RespostaNotificacao>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemaApi), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConsultarHistoricoDeNotificacoes(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        CancellationToken cancelamento)
    {
        var lista = await this.notificacaoService
            .HistoricoAsync(UtilizadorAutenticado.Id(this.User), de, ate, cancelamento)
            .ConfigureAwait(false);
        var calendario = this.relogio.Calendario();
        return this.Ok(lista
            .Select(n => new RespostaNotificacao(
                n.Id,
                n.Tipo,
                n.Mensagem,
                n.FichaId,
                n.NomeFicha,
                n.Medicamento,
                n.PrevistaEmUtc is DateTime previstaUtc ? calendario.ParaLocal(previstaUtc) : null,
                calendario.ParaLocal(n.CriadaEmUtc),
                n.Email))
            .ToList());
    }
}
