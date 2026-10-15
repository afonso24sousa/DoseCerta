// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Contas;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Contas;
using DoseCerta.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Dados da conta e palavra-passe do Utilizador autenticado (F001). Endpoints: OP-04, OP-05 e OP-06.
/// </summary>
[ApiController]
[Route("api/v1/conta")]
public sealed class ContaController : ControllerBase
{
    private readonly ContaService contaService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContaController"/> class.
    /// </summary>
    /// <param name="contaService">Serviço da conta.</param>
    public ContaController(ContaService contaService)
    {
        this.contaService = contaService;
    }

    /// <summary>
    /// OP-04 Consultar dados da conta.
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o nome e o e-mail; nunca inclui a palavra-passe (RF-013).</returns>
    [HttpGet]
    public async Task<IActionResult> ConsultarDadosDaConta(CancellationToken cancelamento)
    {
        var utilizador = await this.contaService.ObterAsync(UtilizadorAutenticado.Id(this.User), cancelamento).ConfigureAwait(false);
        return this.Ok(Resposta(utilizador));
    }

    /// <summary>
    /// OP-05 Atualizar dados da conta.
    /// </summary>
    /// <param name="pedido">Novo nome e novo e-mail.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com os dados atualizados.</returns>
    [HttpPut]
    public async Task<IActionResult> AtualizarDadosDaConta([FromBody] PedidoAtualizacaoConta pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var utilizador = await this.contaService
            .AtualizarAsync(UtilizadorAutenticado.Id(this.User), pedido.Nome, pedido.Email, cancelamento)
            .ConfigureAwait(false);
        return this.Ok(Resposta(utilizador));
    }

    /// <summary>
    /// OP-06 Alterar palavra-passe.
    /// </summary>
    /// <param name="pedido">Palavra-passe atual e nova.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo.</returns>
    [HttpPut("palavra-passe")]
    public async Task<IActionResult> AlterarPalavraPasse([FromBody] PedidoAlteracaoPalavraPasse pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        await this.contaService
            .AlterarPalavraPasseAsync(UtilizadorAutenticado.Id(this.User), pedido.PalavraPasseAtual, pedido.PalavraPasseNova, cancelamento)
            .ConfigureAwait(false);
        return this.NoContent();
    }

    private static RespostaConta Resposta(Utilizador utilizador)
    {
        return new RespostaConta(utilizador.Id, utilizador.Nome, utilizador.Email);
    }
}
