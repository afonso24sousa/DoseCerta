// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos;
using DoseCerta.Api.Dados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Categorias fechadas usadas no preenchimento das fichas.
/// </summary>
[ApiController]
[Authorize]
[Route("api/catalogos")]
[Produces("application/json")]
public class CatalogosController : ControllerBase
{
    private readonly DoseCertaDbContext contexto;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogosController"/> class.
    /// </summary>
    /// <param name="contexto">Contexto de dados.</param>
    public CatalogosController(DoseCertaDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <summary>
    /// Devolve as condições de saúde, alergias e graus de autonomia disponíveis.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>As categorias fechadas.</returns>
    [HttpGet]
    [ProducesResponseType<RespostaCatalogos>(StatusCodes.Status200OK)]
    public async Task<RespostaCatalogos> Obter(CancellationToken cancellationToken)
    {
        var condicoes = await this.contexto.CondicoesSaude.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new RespostaCategoria(c.Id, c.Nome)).ToListAsync(cancellationToken);
        var alergias = await this.contexto.Alergias.AsNoTracking().OrderBy(a => a.Id)
            .Select(a => new RespostaCategoria(a.Id, a.Nome)).ToListAsync(cancellationToken);
        var graus = await this.contexto.GrausAutonomia.AsNoTracking().OrderBy(g => g.Id)
            .Select(g => new RespostaCategoria(g.Id, g.Nome)).ToListAsync(cancellationToken);

        return new RespostaCatalogos(condicoes, alergias, graus);
    }
}
