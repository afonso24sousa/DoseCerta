// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Tomas;

/// <summary>
/// Geração, regeneração e remoção das tomas previstas de um tratamento (RF-060). Nesta versão tem só as operações usadas
/// pela F005 (registar, atualizar e terminar tratamento); a consulta, a confirmação, a sincronização e o processo periódico
/// são da F006 e da F011. Corre dentro da transação de quem a chama.
/// </summary>
public sealed class TomaService
{
    private readonly ITomaRepositorio tomas;
    private readonly IFichaRepositorio fichas;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomaService"/> class.
    /// </summary>
    /// <param name="tomas">Repositório das tomas.</param>
    /// <param name="fichas">Repositório das fichas.</param>
    /// <param name="relogio">Relógio do sistema, com o calendário de Lisboa.</param>
    public TomaService(ITomaRepositorio tomas, IFichaRepositorio fichas, IRelogio relogio)
    {
        this.tomas = tomas;
        this.fichas = fichas;
        this.relogio = relogio;
    }

    /// <summary>
    /// Gera as tomas de um tratamento registado: uma por horário e por dia entre a data de início e a data de fim; sem data
    /// de fim, até ao fim da janela móvel de 30 dias. Não gera instantes anteriores a <see cref="Ficha.AtivaDesdeUtc"/> nem
    /// tomas de fichas arquivadas (RF-060, RF-069, DM-19).
    /// </summary>
    /// <param name="tratamento">Tratamento registado.</param>
    /// <param name="agoraUtc">Instante atual, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O número de tomas geradas.</returns>
    public async Task<int> GerarParaTratamentoAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);

        var calendario = this.relogio.Calendario();
        return await this.GerarAsync(tratamento, calendario.InicioDoDiaUtc(tratamento.DataInicio), agoraUtc, cancelamento)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Volta a gerar as tomas futuras de um tratamento alterado: apaga as pendentes previstas depois de agora e gera-as com os
    /// dados e os horários novos; as anteriores mantêm a hora e o estado (RF-055, RF-060).
    /// </summary>
    /// <param name="tratamento">Tratamento alterado.</param>
    /// <param name="agoraUtc">Instante da alteração, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public async Task RegerarFuturasAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);

        await this.RemoverFuturasAsync(tratamento, agoraUtc, cancelamento).ConfigureAwait(false);
        await this.GerarAsync(tratamento, agoraUtc, agoraUtc, cancelamento).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove as tomas pendentes de um tratamento previstas depois de agora, ao terminá-lo; as anteriores mantêm a hora e o
    /// estado (RF-056, RF-060).
    /// </summary>
    /// <param name="tratamento">Tratamento terminado.</param>
    /// <param name="agoraUtc">Instante do término, em UTC.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public Task RemoverFuturasAsync(Tratamento tratamento, DateTime agoraUtc, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(tratamento);
        return this.tomas.EliminarFuturasPendentesAsync(tratamento.Id, agoraUtc, cancelamento);
    }

    private async Task<int> GerarAsync(Tratamento tratamento, DateTime aPartirDeUtc, DateTime agoraUtc, CancellationToken cancelamento)
    {
        var ficha = await this.fichas.ObterCompletaAsync(tratamento.FichaId, cancelamento).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O tratamento pertence a uma ficha inexistente.");
        if (!ficha.EstaAtiva())
        {
            return 0;
        }

        var calendario = this.relogio.Calendario();
        var deUtc = aPartirDeUtc > ficha.AtivaDesdeUtc ? aPartirDeUtc : ficha.AtivaDesdeUtc;
        var ateUtc = tratamento.DataFim is DateOnly fim
            ? calendario.InicioDoDiaSeguinteUtc(fim)
            : agoraUtc.AddDays(ParametrosDeNegocio.JanelaMovelDias);

        var geradas = 0;
        foreach (var toma in tratamento.GerarTomas(deUtc, ateUtc, calendario))
        {
            if (await this.tomas.AdicionarSeNaoExisteAsync(toma, cancelamento).ConfigureAwait(false))
            {
                geradas++;
            }
        }

        return geradas;
    }
}
