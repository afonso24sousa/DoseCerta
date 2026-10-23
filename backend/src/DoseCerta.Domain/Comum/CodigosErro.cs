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
    /// O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (403, RNF-004).
    /// </summary>
    public const string AcessoProibido = "ACESSO_PROIBIDO";

    /// <summary>
    /// A ficha não existe ou o Utilizador não está associado a ela (404, RNF-005).
    /// </summary>
    public const string FichaNaoEncontrada = "FICHA_NAO_ENCONTRADA";

    /// <summary>
    /// O documento não existe ou não pertence a uma ficha do Utilizador (404, RNF-007).
    /// </summary>
    public const string DocumentoNaoEncontrado = "DOCUMENTO_NAO_ENCONTRADO";

    /// <summary>
    /// A ficha está arquivada; a operação só é possível depois de a reativar (409, RF-120).
    /// </summary>
    public const string FichaArquivada = "FICHA_ARQUIVADA";

    /// <summary>
    /// A ficha já está arquivada (409, OP-12).
    /// </summary>
    public const string FichaJaArquivada = "FICHA_JA_ARQUIVADA";

    /// <summary>
    /// A ficha não está arquivada (409, OP-13).
    /// </summary>
    public const string FichaNaoArquivada = "FICHA_NAO_ARQUIVADA";

    /// <summary>
    /// A ficha tem medicação, tarefas ou histórico e só pode ser arquivada (409, RF-123).
    /// </summary>
    public const string FichaComHistorico = "FICHA_COM_HISTORICO";

    /// <summary>
    /// O Utilizador já tem uma ficha própria (409, RF-031).
    /// </summary>
    public const string FichaPropriaJaExiste = "FICHA_PROPRIA_JA_EXISTE";

    /// <summary>
    /// O titular da ficha própria é sempre o Cuidador principal: não é despromovido, removido nem substituído (403, RF-033, RF-034).
    /// </summary>
    public const string CuidadorPrincipalProtegido = "CUIDADOR_PRINCIPAL_PROTEGIDO";

    /// <summary>
    /// Ficheiro com mais de 10 MB (413, RNF-008).
    /// </summary>
    public const string FicheiroDemasiadoGrande = "FICHEIRO_DEMASIADO_GRANDE";

    /// <summary>
    /// Formato diferente de PDF, JPG ou PNG (415, RNF-008).
    /// </summary>
    public const string FormatoNaoSuportado = "FORMATO_NAO_SUPORTADO";

    /// <summary>
    /// O ficheiro não pôde ser guardado; nada é gravado (503, RF-023).
    /// </summary>
    public const string DocumentoNaoGuardado = "DOCUMENTO_NAO_GUARDADO";

    /// <summary>
    /// O ficheiro guardado não pôde ser obtido (503, RF-124).
    /// </summary>
    public const string DocumentoIndisponivel = "DOCUMENTO_INDISPONIVEL";

    /// <summary>
    /// Erro inesperado do servidor, sem detalhes internos na resposta (500).
    /// </summary>
    public const string ErroInterno = "ERRO_INTERNO";
}
