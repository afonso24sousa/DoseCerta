// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos e-mails a enviar (EnvioEmailRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface IEnvioEmailRepositorio
{
    /// <summary>
    /// Regista um e-mail a enviar, na transação em curso.
    /// </summary>
    /// <param name="envio">E-mail a enviar.</param>
    void Adicionar(EnvioEmail envio);
}
