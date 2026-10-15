// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso às contas dos Utilizadores (UtilizadorRepositorio do Modelo de Classes do Backend).
/// As comparações de e-mail não distinguem maiúsculas (collation da coluna, RF-011).
/// </summary>
public interface IUtilizadorRepositorio
{
    /// <summary>
    /// Obtém um Utilizador pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do Utilizador.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O Utilizador, ou <see langword="null"/> se não existir.</returns>
    Task<Utilizador?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Obtém um Utilizador pelo e-mail.
    /// </summary>
    /// <param name="email">E-mail da conta.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O Utilizador, ou <see langword="null"/> se não existir.</returns>
    Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken cancelamento);

    /// <summary>
    /// Indica se o e-mail já pertence a uma conta, opcionalmente excluindo uma conta.
    /// </summary>
    /// <param name="email">E-mail a verificar.</param>
    /// <param name="excetoId">Conta a excluir da verificação, ou <see langword="null"/>.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se o e-mail já estiver registado.</returns>
    Task<bool> ExisteEmailAsync(string email, Guid? excetoId, CancellationToken cancelamento);

    /// <summary>
    /// Acrescenta uma conta nova, a gravar na transação em curso.
    /// </summary>
    /// <param name="utilizador">Conta nova.</param>
    void Adicionar(Utilizador utilizador);
}
