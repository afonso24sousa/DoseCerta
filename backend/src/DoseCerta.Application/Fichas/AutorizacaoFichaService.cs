// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Verifica, em cada pedido, a associação e o papel do Utilizador na ficha, lendo o papel da base de dados e nunca do token
/// (RF-047, RF-048, RNF-004, RNF-005; Matriz de Permissões, RP-01 a RP-03 e RP-13).
/// </summary>
public sealed class AutorizacaoFichaService
{
    /// <summary>
    /// Mensagem da resposta 404 quando a ficha não existe ou o Utilizador não está associado a ela.
    /// </summary>
    public const string MensagemFichaNaoEncontrada = "A ficha não existe ou não está associado a ela.";

    private readonly IFichaRepositorio fichas;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutorizacaoFichaService"/> class.
    /// </summary>
    /// <param name="fichas">Repositório das fichas.</param>
    public AutorizacaoFichaService(IFichaRepositorio fichas)
    {
        this.fichas = fichas;
    }

    /// <summary>
    /// Obtém a ficha e a associação do Utilizador, sem recusar.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O acesso, ou <see langword="null"/> se a ficha não existir ou o Utilizador não estiver associado.</returns>
    public async Task<AcessoFicha?> ObterAcessoAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        var ficha = await this.fichas.ObterCompletaAsync(fichaId, cancelamento).ConfigureAwait(false);
        var associacao = ficha?.ObterAssociacao(utilizadorId);
        return ficha is null || associacao is null ? null : new AcessoFicha(ficha, associacao);
    }

    /// <summary>
    /// Obtém as fichas ativas a que o Utilizador está associado, opcionalmente só com um papel, com a associação de cada uma.
    /// Limita a agenda e a lista diária às fichas do Utilizador; as fichas arquivadas e os convites pendentes não contam
    /// (RF-083, RF-100, RNF-005).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="papel">Papel exigido, ou <see langword="null"/> para Cuidadores e Familiares.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O acesso a cada ficha, pelo identificador da ficha; vazio se não houver nenhuma.</returns>
    public async Task<IReadOnlyDictionary<Guid, AcessoFicha>> IdentificadoresDeFichasAtivasAsync(
        Guid utilizadorId,
        Papel? papel,
        CancellationToken cancelamento)
    {
        var lista = await this.fichas.ListarDoUtilizadorAsync(utilizadorId, EstadoFicha.Ativa, cancelamento).ConfigureAwait(false);
        var acessos = new Dictionary<Guid, AcessoFicha>();
        foreach (var ficha in lista)
        {
            var associacao = ficha.ObterAssociacao(utilizadorId);
            if (associacao is not null && (papel is null || associacao.Papel == papel))
            {
                acessos[ficha.Id] = new AcessoFicha(ficha, associacao);
            }
        }

        return acessos;
    }

    /// <summary>
    /// Exige que o Utilizador esteja associado à ficha, com qualquer papel. O Utilizador com convite pendente não está associado.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O acesso à ficha.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaNaoEncontrada"/> (404), sem revelar se a ficha existe.</exception>
    public async Task<AcessoFicha> ExigirConsultaAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        return await this.ObterAcessoAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false)
            ?? throw new DominioException(CodigosErro.FichaNaoEncontrada, MensagemFichaNaoEncontrada);
    }

    /// <summary>
    /// Exige que o Utilizador seja Cuidador (ou Cuidador principal) da ficha. A ficha pode estar arquivada.
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O acesso à ficha.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaNaoEncontrada"/> (404) ou <see cref="CodigosErro.AcessoProibido"/> (403).</exception>
    public async Task<AcessoFicha> ExigirCuidadorAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        var acesso = await this.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        if (!acesso.Associacao.PodeEscrever())
        {
            throw new DominioException(
                CodigosErro.AcessoProibido,
                "O seu papel na ficha não permite esta operação: o Familiar só consulta os dados da ficha.");
        }

        return acesso;
    }

    /// <summary>
    /// Exige que o Utilizador seja Cuidador de uma ficha ativa, nas operações de escrita recusadas numa ficha arquivada (RP-13).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O acesso à ficha.</returns>
    /// <exception cref="DominioException">Com 404, 403 ou <see cref="CodigosErro.FichaArquivada"/> (409), por esta ordem.</exception>
    public async Task<AcessoFicha> ExigirCuidadorDeFichaAtivaAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        var acesso = await this.ExigirCuidadorAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        acesso.Ficha.ExigirAtiva();
        return acesso;
    }
}
