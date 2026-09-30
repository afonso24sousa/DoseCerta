// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Papel de um Utilizador numa ficha.
/// </summary>
public enum PapelFicha
{
    /// <summary>
    /// Acesso completo: cria, regista e confirma dados e tarefas.
    /// </summary>
    Cuidador = 1,

    /// <summary>
    /// Acesso de leitura: consulta a situação sem intervir.
    /// </summary>
    Familiar = 2,
}
