// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Registos;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Ocorrencias;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Ocorrências diárias e notas partilhadas (F009). Endpoints: OP-46, OP-47, OP-48 e OP-49.
/// </summary>
/// <remarks>
/// Os registos são imutáveis: <c>PUT</c>, <c>PATCH</c> e <c>DELETE</c> em <c>/api/v1/fichas/{fichaId}/registos/{registoId}</c>
/// respondem 405 <c>REGISTO_IMUTAVEL</c> e não correspondem a operações (Documentação da API, secção 4).
/// </remarks>
[ApiController]
[Route("api/v1")]
public sealed class RegistosController : ControllerBase
{
    private readonly OcorrenciaService ocorrenciaService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistosController"/> class.
    /// </summary>
    /// <param name="ocorrenciaService">Serviço das ocorrências e das notas.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar os instantes com o desvio de Lisboa.</param>
    public RegistosController(OcorrenciaService ocorrenciaService, IRelogio relogio)
    {
        this.ocorrenciaService = ocorrenciaService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-46 Registar ocorrência.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">Descrição e instante em que a ocorrência aconteceu.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com a ocorrência registada, o autor e o instante do registo (RF-092).</returns>
    [HttpPost("fichas/{fichaId:guid}/ocorrencias")]
    public async Task<IActionResult> RegistarOcorrencia(Guid fichaId, [FromBody] PedidoOcorrencia pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var ocorrencia = await this.ocorrenciaService
            .RegistarOcorrenciaAsync(UtilizadorAutenticado.Id(this.User), fichaId, pedido.Descricao, pedido.OcorridaEm?.UtcDateTime, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, this.Resposta(ocorrencia));
    }

    /// <summary>
    /// OP-47 Registar nota partilhada.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="pedido">Texto da nota, no campo <c>descricao</c>.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com a nota registada, o autor e o instante do registo (RF-092).</returns>
    [HttpPost("fichas/{fichaId:guid}/notas")]
    public async Task<IActionResult> RegistarNotaPartilhada(Guid fichaId, [FromBody] PedidoNota pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var nota = await this.ocorrenciaService
            .RegistarNotaAsync(UtilizadorAutenticado.Id(this.User), fichaId, pedido.Descricao, cancelamento)
            .ConfigureAwait(false);
        return this.StatusCode(StatusCodes.Status201Created, this.Resposta(nota));
    }

    /// <summary>
    /// OP-48 Consultar ocorrências (<c>tipo=Ocorrencia</c>) e OP-49 Consultar notas partilhadas (<c>tipo=Nota</c>).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="tipo">Tipo dos registos: <c>Ocorrencia</c> ou <c>Nota</c>; obrigatório.</param>
    /// <param name="de">Filtro opcional: primeiro dia do registo (AAAA-MM-DD, hora de Lisboa).</param>
    /// <param name="ate">Filtro opcional: último dia do registo (AAAA-MM-DD, hora de Lisboa), inclusivo.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os registos, do mais recente para o mais antigo; as notas dão 403 ao Familiar (RF-095).</returns>
    [HttpGet("fichas/{fichaId:guid}/registos")]
    public async Task<IActionResult> ConsultarRegistos(
        Guid fichaId,
        [FromQuery] TipoRegisto? tipo,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        CancellationToken cancelamento)
    {
        if (tipo is null || !Enum.IsDefined(tipo.Value))
        {
            throw ValidacaoException.DoCampo("tipo", "É obrigatório e tem de ser Ocorrencia ou Nota.");
        }

        var utilizadorId = UtilizadorAutenticado.Id(this.User);
        var registos = tipo == TipoRegisto.Nota
            ? await this.ocorrenciaService.ConsultarNotasAsync(utilizadorId, fichaId, de, ate, cancelamento).ConfigureAwait(false)
            : await this.ocorrenciaService.ConsultarOcorrenciasAsync(utilizadorId, fichaId, de, ate, cancelamento).ConfigureAwait(false);
        return this.Ok(registos.Select(this.Resposta).ToList());
    }

    /// <summary>
    /// Recusa a alteração ou a eliminação de uma ocorrência ou de uma nota (RF-090, RF-091, RNF-049).
    /// </summary>
    /// <returns>Nunca devolve: responde sempre 405 <c>REGISTO_IMUTAVEL</c>.</returns>
    /// <exception cref="DominioException">Sempre, com <see cref="CodigosErro.RegistoImutavel"/>.</exception>
    [HttpPut("fichas/{fichaId:guid}/registos/{registoId:guid}")]
    [HttpPatch("fichas/{fichaId:guid}/registos/{registoId:guid}")]
    [HttpDelete("fichas/{fichaId:guid}/registos/{registoId:guid}")]
    public IActionResult RecusarAlteracaoDeRegisto()
    {
        throw new DominioException(CodigosErro.RegistoImutavel, "As ocorrências e as notas não podem ser alteradas nem eliminadas.");
    }

    private RespostaRegisto Resposta(RegistoPartilhado registo)
    {
        var calendario = this.relogio.Calendario();
        return new RespostaRegisto(
            registo.Id,
            registo.FichaId,
            registo.Tipo,
            registo.Descricao,
            new RespostaAutor(registo.AutorId, registo.NomeAutor),
            registo.OcorreuEmUtc is null ? null : calendario.ParaLocal(registo.OcorreuEmUtc.Value),
            calendario.ParaLocal(registo.RegistadaEmUtc));
    }
}
