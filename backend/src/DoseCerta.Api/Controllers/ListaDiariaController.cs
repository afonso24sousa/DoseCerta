// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using DoseCerta.Api.Contratos.ListaDiaria;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.ListaDiaria;
using DoseCerta.Domain.Comum;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Lista de tarefas diária consolidada (F010). Endpoints: OP-50.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class ListaDiariaController : ControllerBase
{
    private readonly ListaDiariaService listaDiariaService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListaDiariaController"/> class.
    /// </summary>
    /// <param name="listaDiariaService">Serviço da lista diária.</param>
    /// <param name="relogio">Relógio do sistema, para apresentar os instantes com o desvio de Lisboa.</param>
    public ListaDiariaController(ListaDiariaService listaDiariaService, IRelogio relogio)
    {
        this.listaDiariaService = listaDiariaService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-50 Consultar lista de tarefas diária.
    /// </summary>
    /// <param name="data">Dia pedido, no formato <c>AAAA-MM-DD</c>; sem ele, o dia de hoje na hora de Lisboa (RNF-047).</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os itens do dia ordenados por hora; lista vazia se o Utilizador não for Cuidador de nenhuma ficha ativa.</returns>
    [HttpGet("lista-diaria")]
    public async Task<IActionResult> ConsultarListaDeTarefasDiaria([FromQuery] string? data, CancellationToken cancelamento)
    {
        DateOnly? dia = null;
        if (data is not null)
        {
            if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lido))
            {
                throw ValidacaoException.DoCampo("data", "Tem de ser uma data válida no formato AAAA-MM-DD.");
            }

            dia = lido;
        }

        var itens = await this.listaDiariaService
            .ConsultarAsync(UtilizadorAutenticado.Id(this.User), dia, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(itens.Select(this.Resposta).ToList());
    }

    private RespostaItemListaDiaria Resposta(ItemListaDiaria item)
    {
        var calendario = this.relogio.Calendario();
        return new RespostaItemListaDiaria(
            item.Tipo,
            item.Id,
            item.FichaId,
            item.NomeFicha,
            item.Titulo,
            calendario.ParaLocal(item.PrevistaEmUtc),
            item.Estado,
            item.ConfirmadaPor,
            item.ConfirmadaEmUtc is DateTime confirmadaEmUtc ? calendario.ParaLocal(confirmadaEmUtc) : null,
            item.Confirmavel);
    }
}
