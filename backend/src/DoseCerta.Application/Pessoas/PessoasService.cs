// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;

namespace DoseCerta.Application.Pessoas;

/// <summary>
/// Gestão das pessoas associadas a uma ficha: listar, alterar o papel, remover o acesso e transferir o Cuidador principal (F004).
/// As regras dos papéis são aplicadas pela <see cref="Domain.Entidades.Ficha"/> (RF-049, RF-130 a RF-134).
/// </summary>
public sealed class PessoasService
{
    private readonly IFichaRepositorio fichas;
    private readonly AutorizacaoFichaService autorizacao;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessoasService"/> class.
    /// </summary>
    /// <param name="fichas">Repositório das fichas.</param>
    /// <param name="autorizacao">Verificação do papel na ficha.</param>
    public PessoasService(IFichaRepositorio fichas, AutorizacaoFichaService autorizacao)
    {
        this.fichas = fichas;
        this.autorizacao = autorizacao;
    }

    /// <summary>
    /// OP-18: lista os Cuidadores e os Familiares da ficha, com o papel e a indicação do Cuidador principal e se é provisório (RF-025).
    /// Qualquer pessoa associada a consulta, também numa ficha arquivada (DP-03, RP-13).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="fichaId">Ficha pedida.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>As pessoas associadas, com o Cuidador principal primeiro.</returns>
    public async Task<IReadOnlyList<PessoaAssociada>> ListarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancelamento)
    {
        await this.autorizacao.ExigirConsultaAsync(utilizadorId, fichaId, cancelamento).ConfigureAwait(false);
        return await this.fichas.ListarPessoasAsync(fichaId, cancelamento).ConfigureAwait(false);
    }
}
