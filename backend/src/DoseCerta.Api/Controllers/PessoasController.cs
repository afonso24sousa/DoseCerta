// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Pessoas;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Pessoas;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Pessoas associadas a uma ficha, alteração de papel, remoção de acesso e transferência do Cuidador principal (F004).
/// Endpoints: OP-18, OP-23 e OP-24.
/// </summary>
[ApiController]
[Route("api/v1/fichas/{fichaId:guid}")]
public sealed class PessoasController : ControllerBase
{
    private readonly PessoasService pessoasService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasController"/> class.
    /// </summary>
    /// <param name="pessoasService">Serviço das pessoas associadas.</param>
    public PessoasController(PessoasService pessoasService)
    {
        this.pessoasService = pessoasService;
    }

    /// <summary>
    /// OP-18 Listar pessoas associadas à ficha.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os Cuidadores e os Familiares da ficha; 404 a quem não está associado (RNF-005).</returns>
    [HttpGet("pessoas")]
    public async Task<IActionResult> ListarPessoasAssociadasAFicha(Guid fichaId, CancellationToken cancelamento)
    {
        var pessoas = await this.pessoasService.ListarAsync(UtilizadorAutenticado.Id(this.User), fichaId, cancelamento).ConfigureAwait(false);
        return this.Ok(pessoas.Select(Resposta).ToList());
    }

    /// <summary>
    /// OP-23 Promover Familiar a Cuidador e OP-24 Despromover Cuidador a Familiar: o corpo indica o papel pretendido.
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="utilizadorId">Identificador da pessoa cujo papel muda.</param>
    /// <param name="pedido">Papel pretendido.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com a pessoa e o novo papel.</returns>
    [HttpPut("pessoas/{utilizadorId:guid}/papel")]
    public async Task<IActionResult> AlterarPapelDeUmaPessoa(
        Guid fichaId,
        Guid utilizadorId,
        [FromBody] PedidoPapel pedido,
        CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var pessoa = await this.pessoasService
            .AlterarPapelAsync(UtilizadorAutenticado.Id(this.User), fichaId, utilizadorId, pedido.Papel, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(pessoa));
    }

    /// <summary>
    /// Converte uma pessoa associada na resposta da API.
    /// </summary>
    /// <param name="pessoa">Pessoa associada.</param>
    /// <returns>A resposta com os dados da pessoa.</returns>
    internal static RespostaPessoa Resposta(PessoaAssociada pessoa)
    {
        return new RespostaPessoa(pessoa.UtilizadorId, pessoa.Nome, pessoa.Email, pessoa.Papel, pessoa.Principal, pessoa.Provisorio);
    }
}
