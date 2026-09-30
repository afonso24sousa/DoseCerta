// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Estado de uma ficha.
/// </summary>
public enum EstadoFicha
{
    /// <summary>
    /// Ficha em utilização.
    /// </summary>
    Ativa = 1,

    /// <summary>
    /// Ficha arquivada: preserva o histórico e deixa de gerar tarefas e alarmes.
    /// </summary>
    Arquivada = 2,
}
