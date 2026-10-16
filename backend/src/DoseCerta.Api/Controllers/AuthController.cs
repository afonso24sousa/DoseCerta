// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Contratos.Contas;
using DoseCerta.Api.Seguranca;
using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoseCerta.Api.Controllers;

/// <summary>
/// Criação de conta, início e término de sessão (F001). As duas primeiras operações são as únicas sem sessão
/// (RF-017). Endpoints: OP-01, OP-02 e OP-03.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService authService;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">Serviço de autenticação.</param>
    /// <param name="relogio">Relógio, para apresentar os instantes com o desvio de Lisboa.</param>
    public AuthController(AuthService authService, IRelogio relogio)
    {
        this.authService = authService;
        this.relogio = relogio;
    }

    /// <summary>
    /// OP-01 Registar conta.
    /// </summary>
    /// <param name="pedido">Nome, e-mail e palavra-passe.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>201 com a conta criada e o número de convites pendentes.</returns>
    [HttpPost("registo")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistarConta([FromBody] PedidoRegisto pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var resultado = await this.authService
            .RegistarAsync(pedido.Nome, pedido.Email, pedido.PalavraPasse, cancelamento)
            .ConfigureAwait(false);
        var resposta = new RespostaRegisto(
            resultado.Utilizador.Id,
            resultado.Utilizador.Nome,
            resultado.Utilizador.Email,
            resultado.ConvitesPendentes);
        return this.StatusCode(StatusCodes.Status201Created, resposta);
    }

    /// <summary>
    /// OP-02 Iniciar sessão.
    /// </summary>
    /// <param name="pedido">E-mail e palavra-passe.</param>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>200 com o token JWT, o tipo e o instante de expiração.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> IniciarSessao([FromBody] PedidoLogin pedido, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var sessao = await this.authService.AutenticarAsync(pedido.Email, pedido.PalavraPasse, cancelamento).ConfigureAwait(false);
        var expiraEm = this.relogio.Calendario().ParaLocal(sessao.ExpiraEmUtc);
        return this.Ok(new RespostaSessao(sessao.Token, "Bearer", expiraEm));
    }

    /// <summary>
    /// OP-03 Terminar sessão: o token deixa de ser aceite (RF-016).
    /// </summary>
    /// <param name="cancelamento">Token de cancelamento do pedido.</param>
    /// <returns>204 sem corpo.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> TerminarSessao(CancellationToken cancelamento)
    {
        await this.authService
            .TerminarSessaoAsync(
                UtilizadorAutenticado.Jti(this.User),
                UtilizadorAutenticado.Id(this.User),
                UtilizadorAutenticado.ExpiraEmUtc(this.User),
                cancelamento)
            .ConfigureAwait(false);
        return this.NoContent();
    }
}
