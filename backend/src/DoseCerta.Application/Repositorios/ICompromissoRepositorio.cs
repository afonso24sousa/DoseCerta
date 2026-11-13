// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Repositorios;

/// <summary>
/// Acesso aos compromissos de saúde (CompromissoRepositorio do Modelo de Classes do Backend).
/// </summary>
public interface ICompromissoRepositorio
{
    /// <summary>
    /// Obtém um compromisso para consulta ou alteração.
    /// </summary>
    /// <param name="id">Identificador do compromisso.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O compromisso, ou <see langword="null"/> se não existir.</returns>
    Task<Compromisso?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    /// <summary>
    /// Lista os compromissos de uma ficha, opcionalmente num intervalo, por ordem da data e hora (OP-42).
    /// </summary>
    /// <param name="fichaId">Identificador da ficha.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC; <see langword="null"/> sem limite.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC; <see langword="null"/> sem limite.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos da ficha.</returns>
    Task<IReadOnlyList<Compromisso>> DaFichaAsync(Guid fichaId, DateTime? deUtc, DateTime? ateUtc, CancellationToken cancelamento);

    /// <summary>
    /// Lista os compromissos de várias fichas num intervalo, numa só consulta, por ordem da data e hora; usado pela agenda
    /// (OP-45) e pela lista diária (OP-50).
    /// </summary>
    /// <param name="fichaIds">Fichas pedidas.</param>
    /// <param name="deUtc">Início do intervalo, inclusive, em UTC.</param>
    /// <param name="ateUtc">Fim do intervalo, exclusive, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os compromissos das fichas no intervalo.</returns>
    Task<IReadOnlyList<Compromisso>> DoPeriodoDasFichasAsync(
        IReadOnlyCollection<Guid> fichaIds,
        DateTime deUtc,
        DateTime ateUtc,
        CancellationToken cancelamento);

    /// <summary>
    /// Lista os identificadores dos compromissos com o aviso por enviar (RF-117): o aviso ainda não foi enviado, o instante do
    /// aviso já chegou mas a data do compromisso não, a ficha está ativa e o aviso não ficou devido antes da última
    /// reativação da ficha (<see cref="Compromisso.InstanteAvisoDevido"/>, RF-142). Os compromissos excluídos nunca são marcados: deixam de ser lidos quando a data passa.
    /// </summary>
    /// <param name="agoraUtc">Instante do ciclo, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os identificadores, por ordem da data e hora do compromisso.</returns>
    Task<IReadOnlyList<Guid>> PorAvisarAsync(DateTime agoraUtc, CancellationToken cancelamento);

    /// <summary>
    /// Marca o aviso como enviado só se o compromisso ainda tiver o aviso por enviar e a mesma data e antecedência com que foi
    /// lido, numa só instrução condicional (RNF-043, RNF-044). Assim, dois ciclos em simultâneo não avisam duas vezes e um
    /// compromisso editado depois da leitura não fica marcado com o aviso dos valores antigos.
    /// </summary>
    /// <param name="lido">Compromisso tal como foi lido na transação.</param>
    /// <param name="agoraUtc">Instante do envio, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se o aviso ficou marcado por esta chamada.</returns>
    Task<bool> MarcarAvisoEnviadoAsync(Compromisso lido, DateTime agoraUtc, CancellationToken cancelamento);

    /// <summary>
    /// Garante que a gravação de um compromisso editado escreve o aviso por enviar, mesmo que já estivesse por enviar quando
    /// foi lido; assim, um aviso marcado entretanto pelo processo periódico com os valores antigos é reposto (RF-081, RF-117).
    /// </summary>
    /// <param name="compromisso">Compromisso editado, com o aviso reposto por <see cref="Compromisso.Atualizar"/>.</param>
    void GravarAvisoReposto(Compromisso compromisso);

    /// <summary>
    /// Regista um compromisso novo.
    /// </summary>
    /// <param name="compromisso">Compromisso a registar.</param>
    void Adicionar(Compromisso compromisso);

    /// <summary>
    /// Remove um compromisso; o aviso deixa de ser emitido (RF-082).
    /// </summary>
    /// <param name="compromisso">Compromisso a remover.</param>
    void Remover(Compromisso compromisso);
}
