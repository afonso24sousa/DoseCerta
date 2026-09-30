// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos;
using DoseCerta.Api.Dominio;
using DoseCerta.Api.Infraestrutura;
using DoseCerta.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Gestão de fichas (F002 e F003).
/// </summary>
[ApiController]
[Authorize]
[Route("api/fichas")]
[Produces("application/json")]
public class FichasController : ControllerBase
{
    private readonly IServicoFichas servico;

    /// <summary>
    /// Initializes a new instance of the <see cref="FichasController"/> class.
    /// </summary>
    /// <param name="servico">Serviço de fichas.</param>
    public FichasController(IServicoFichas servico)
    {
        this.servico = servico;
    }

    /// <summary>
    /// Cria uma ficha. Com <c>paraOUtilizadorAutenticado=true</c> cria a ficha própria (F003) e o Utilizador
    /// fica imediatamente Cuidador principal. Se algum documento não for guardado, nada é gravado.
    /// </summary>
    /// <param name="pedido">Dados da ficha e documentos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha criada.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit((RegrasDocumentos.MaximoPorPedido * RegrasDocumentos.TamanhoMaximoBytes) + (1024 * 1024))]
    [RequestFormLimits(MultipartBodyLengthLimit = (RegrasDocumentos.MaximoPorPedido * RegrasDocumentos.TamanhoMaximoBytes) + (1024 * 1024))]
    [ProducesResponseType<RespostaFicha>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RespostaFicha>> Criar([FromForm] PedidoCriacaoFicha pedido, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        var novaFicha = new NovaFicha(
            pedido.ParaOUtilizadorAutenticado,
            new DadosIdentificacaoFicha(
                pedido.NomeCompleto!.Trim(),
                pedido.DataNascimento!.Value,
                pedido.ContactoEmergenciaNome!.Trim(),
                pedido.ContactoEmergenciaTelefone!.Trim(),
                pedido.GrauAutonomiaId!.Value),
            pedido.CondicoesSaudeIds.ToList(),
            pedido.AlergiasIds.ToList(),
            pedido.Documentos
                .Select(d => new NovoDocumento(
                    d.Categoria!.Value,
                    d.Ficheiro!.FileName,
                    d.Ficheiro.ContentType,
                    d.Ficheiro.Length,
                    d.Ficheiro.OpenReadStream))
                .ToList());

        var ficha = await this.servico.CriarAsync(this.User.ObterId(), novaFicha, cancellationToken);
        return this.CreatedAtAction(nameof(this.Obter), new { id = ficha.Id }, RespostaFicha.De(ficha));
    }

    /// <summary>
    /// Obtém a ficha própria do Utilizador autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha própria, ou 404 se ainda não existir.</returns>
    [HttpGet("propria")]
    [ProducesResponseType<RespostaFicha>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespostaFicha>> ObterPropria(CancellationToken cancellationToken)
    {
        var ficha = await this.servico.ObterPropriaAsync(this.User.ObterId(), cancellationToken);
        return ficha is null ? this.NotFound() : RespostaFicha.De(ficha);
    }

    /// <summary>
    /// Obtém uma ficha à qual o Utilizador autenticado está associado.
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RespostaFicha>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RespostaFicha>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var ficha = await this.servico.ObterAsync(this.User.ObterId(), id, cancellationToken);
        return RespostaFicha.De(ficha);
    }

    /// <summary>
    /// Elimina uma ficha sem medicação, tarefas nem histórico; caso contrário devolve 409 e a ficha só pode ser arquivada.
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sem conteúdo.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await this.servico.EliminarAsync(this.User.ObterId(), id, cancellationToken);
        return this.NoContent();
    }

    /// <summary>
    /// Arquiva uma ficha: preserva o histórico e deixa de gerar tarefas e alarmes.
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sem conteúdo.</returns>
    [HttpPost("{id:guid}/arquivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Arquivar(Guid id, CancellationToken cancellationToken)
    {
        await this.servico.ArquivarAsync(this.User.ObterId(), id, cancellationToken);
        return this.NoContent();
    }

    /// <summary>
    /// Reativa uma ficha arquivada.
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sem conteúdo.</returns>
    [HttpPost("{id:guid}/reativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken cancellationToken)
    {
        await this.servico.ReativarAsync(this.User.ObterId(), id, cancellationToken);
        return this.NoContent();
    }

    /// <summary>
    /// Altera o papel de uma pessoa associada. Na ficha própria, o dono não pode ser despromovido (409).
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="utilizadorId">Utilizador cujo papel é alterado.</param>
    /// <param name="pedido">Novo papel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sem conteúdo.</returns>
    [HttpPut("{id:guid}/associacoes/{utilizadorId:guid}/papel")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AlterarPapel(Guid id, Guid utilizadorId, [FromBody] PedidoAlteracaoPapel pedido, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        await this.servico.AlterarPapelAsync(this.User.ObterId(), id, utilizadorId, pedido.Papel!.Value, cancellationToken);
        return this.NoContent();
    }

    /// <summary>
    /// Remove uma pessoa associada. Na ficha própria, o dono não pode ser removido (409).
    /// </summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="utilizadorId">Utilizador a remover.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sem conteúdo.</returns>
    [HttpDelete("{id:guid}/associacoes/{utilizadorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoverAssociacao(Guid id, Guid utilizadorId, CancellationToken cancellationToken)
    {
        await this.servico.RemoverAssociacaoAsync(this.User.ObterId(), id, utilizadorId, cancellationToken);
        return this.NoContent();
    }
}
