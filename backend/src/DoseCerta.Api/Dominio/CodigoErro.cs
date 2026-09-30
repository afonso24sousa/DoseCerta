// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Códigos das violações de regras de negócio, devolvidos ao cliente no campo <c>codigo</c>.
/// </summary>
public enum CodigoErro
{
    /// <summary>
    /// O Utilizador autenticado não existe no sistema.
    /// </summary>
    UtilizadorNaoEncontrado = 1,

    /// <summary>
    /// A ficha não existe ou o Utilizador não lhe está associado.
    /// </summary>
    FichaNaoEncontrada,

    /// <summary>
    /// O Utilizador não tem o papel necessário para a operação.
    /// </summary>
    AcessoNegado,

    /// <summary>
    /// O Utilizador já tem uma ficha própria (F003).
    /// </summary>
    FichaPropriaJaExiste,

    /// <summary>
    /// Tentativa de despromover ou remover o dono da ficha própria (F003).
    /// </summary>
    CuidadorPrincipalFichaPropriaImutavel,

    /// <summary>
    /// O Utilizador já está associado à ficha.
    /// </summary>
    AssociacaoJaExiste,

    /// <summary>
    /// O Utilizador não está associado à ficha.
    /// </summary>
    AssociacaoNaoEncontrada,

    /// <summary>
    /// A ficha tem medicação, tarefas ou histórico e só pode ser arquivada.
    /// </summary>
    FichaComHistorico,

    /// <summary>
    /// A operação não é permitida no estado atual da ficha.
    /// </summary>
    EstadoFichaInvalido,

    /// <summary>
    /// Não foi possível guardar um documento; nenhum dado foi guardado.
    /// </summary>
    FalhaGravacaoDocumento,

    /// <summary>
    /// Funcionalidade pertencente a outra Issue ainda não implementada.
    /// </summary>
    NaoImplementado,
}
