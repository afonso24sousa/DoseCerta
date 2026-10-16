// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às credenciais terminadas (SessaoRevogadaRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface ISessaoRevogadaRepositorio
{
    /// <summary>
    /// Indica se a credencial foi terminada.
    /// </summary>
    /// <param name="jti">Identificador da credencial.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se a credencial estiver terminada.</returns>
    Task<bool> EstaRevogadaAsync(Guid jti, CancellationToken cancelamento);

    /// <summary>
    /// Acrescenta uma credencial terminada, a gravar na transação em curso.
    /// </summary>
    /// <param name="sessao">Credencial terminada.</param>
    void Adicionar(SessaoRevogada sessao);

    /// <summary>
    /// Elimina as credenciais terminadas cuja validade já expirou.
    /// </summary>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de credenciais eliminadas.</returns>
    Task<int> EliminarExpiradasAsync(DateTime agoraUtc, CancellationToken cancelamento);
}
