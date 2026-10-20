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
    /// E-mail inexistente ou palavra-passe errada (401).
    /// </summary>
    public const string CredenciaisInvalidas = "CREDENCIAIS_INVALIDAS";

    /// <summary>
    /// Pedido sem token, com token expirado, alterado ou terminado (401).
    /// </summary>
    public const string NaoAutenticado = "NAO_AUTENTICADO";

    /// <summary>
    /// A palavra-passe atual indicada está errada (403).
    /// </summary>
    public const string PalavraPasseAtualIncorreta = "PALAVRA_PASSE_ATUAL_INCORRETA";

    /// <summary>
    /// Já existe uma conta com o e-mail indicado (409).
    /// </summary>
    public const string EmailJaRegistado = "EMAIL_JA_REGISTADO";

    /// <summary>
    /// Erro inesperado do servidor, sem detalhes internos na resposta (500).
    /// </summary>
    public const string ErroInterno = "ERRO_INTERNO";
}
