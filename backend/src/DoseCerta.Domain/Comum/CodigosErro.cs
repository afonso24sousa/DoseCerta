// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Códigos de erro da API (Documentação da API, secção 3.3; Modelo de Classes do Backend, secção 11.3).
/// </summary>
public static class CodigosErro
{
    /// <summary>
    /// Os dados submetidos são inválidos (400).
    /// </summary>
    public const string ValidacaoInvalida = "VALIDACAO_INVALIDA";

    /// <summary>
    /// Erro inesperado do servidor, sem detalhes internos na resposta (500).
    /// </summary>
    public const string ErroInterno = "ERRO_INTERNO";
}
