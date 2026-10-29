// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Application.Tomas;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.Application.ListaDiaria;

/// <summary>
/// Lista de tarefas diária consolidada (F010): reúne numa só consulta as tomas e os compromissos do dia das fichas ativas em
/// que o Utilizador é Cuidador, incluindo as tomas do dia anterior que continuam pendentes quando o dia é hoje. As fichas
/// arquivadas e as fichas em que é só Familiar não aparecem (RF-100, RF-101, RF-104; Matriz de Permissões, RP-17).
/// </summary>
public sealed class ListaDiariaService
{
    // Uma toma sem confirmação 24 horas depois da hora prevista já não pode ser confirmada (RF-068).
    private static readonly TimeSpan LimiteConfirmacao = TimeSpan.FromHours(24);

    private readonly ITomaRepositorio tomas;
    private readonly ICompromissoRepositorio compromissos;
    private readonly AutorizacaoFichaService autorizacao;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListaDiariaService"/> class.
    /// </summary>
    /// <param name="tomas">Repositório das tomas.</param>
    /// <param name="compromissos">Repositório dos compromissos.</param>
    /// <param name="autorizacao">Serviço de autorização por ficha.</param>
    /// <param name="relogio">Relógio do sistema, com o calendário de Lisboa.</param>
    public ListaDiariaService(
        ITomaRepositorio tomas,
        ICompromissoRepositorio compromissos,
        AutorizacaoFichaService autorizacao,
        IRelogio relogio)
    {
        this.tomas = tomas;
        this.compromissos = compromissos;
        this.autorizacao = autorizacao;
        this.relogio = relogio;
    }

    /// <summary>
    /// Consulta a lista diária de um Cuidador, ordenada por hora (OP-50).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="data">Dia pedido, em hora de Lisboa; <see langword="null"/> para o dia de hoje (RNF-047).</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os itens do dia; vazio se o Utilizador não for Cuidador de nenhuma ficha ativa.</returns>
    public async Task<IReadOnlyList<ItemListaDiaria>> ConsultarAsync(Guid utilizadorId, DateOnly? data, CancellationToken cancelamento)
    {
        var fichas = await this.autorizacao.IdentificadoresDeFichasAtivasAsync(utilizadorId, Papel.Cuidador, cancelamento)
            .ConfigureAwait(false);
        if (fichas.Count == 0)
        {
            return Array.Empty<ItemListaDiaria>();
        }

        var fichaIds = fichas.Keys.ToList();

        var agoraUtc = this.relogio.AgoraUtc();
        var calendario = this.relogio.Calendario();
        var hoje = calendario.DiaLocal(agoraUtc);
        var dia = data ?? hoje;
        var inicioUtc = calendario.InicioDoDiaUtc(dia);
        var fimUtc = calendario.InicioDoDiaSeguinteUtc(dia);

        // Regra do grupo (RF-100, CA4): a lista de hoje inclui também as tomas do dia anterior que continuam pendentes.
        var inicioTomasUtc = dia == hoje ? calendario.InicioDoDiaUtc(dia.AddDays(-1)) : inicioUtc;

        var tomasDoDia = await this.tomas.DoDiaDosCuidadoresAsync(fichaIds, inicioTomasUtc, fimUtc, cancelamento)
            .ConfigureAwait(false);
        var compromissosDoDia = await this.compromissos.DoPeriodoDasFichasAsync(fichaIds, inicioUtc, fimUtc, cancelamento)
            .ConfigureAwait(false);

        return tomasDoDia
            .Where(t => t.PrevistaEmUtc >= inicioUtc || t.Estado == EstadoToma.Pendente)
            .Select(t => ItemDeToma(t, hoje, agoraUtc, calendario))
            .Concat(compromissosDoDia.Select(c => ItemDeCompromisso(c, fichas[c.FichaId].Ficha.NomeCompleto)))
            .OrderBy(i => i.PrevistaEmUtc)
            .ThenBy(i => i.Tipo)
            .ThenBy(i => i.NomeFicha, StringComparer.Ordinal)
            .ThenBy(i => i.Titulo, StringComparer.Ordinal)
            .ThenBy(i => i.Id)
            .ToList();
    }

    // Só se confirma uma toma pendente a partir das 00:00 do dia previsto e antes de passar a não confirmada (RF-064, RF-068).
    private static ItemListaDiaria ItemDeToma(TomaDoDia toma, DateOnly hoje, DateTime agoraUtc, CalendarioLisboa calendario)
    {
        var confirmavel = toma.Estado == EstadoToma.Pendente
            && calendario.DiaLocal(toma.PrevistaEmUtc) <= hoje
            && agoraUtc < toma.PrevistaEmUtc + LimiteConfirmacao;
        return new ItemListaDiaria(
            TipoItemListaDiaria.Toma,
            toma.Id,
            toma.FichaId,
            toma.NomeFicha,
            $"{toma.Medicamento} {toma.Dose}",
            toma.PrevistaEmUtc,
            toma.Estado,
            toma.ConfirmadaPor,
            toma.ConfirmadaEmUtc,
            confirmavel);
    }

    // Os compromissos são só de consulta: não têm estado nem podem ser confirmados (RF-087).
    private static ItemListaDiaria ItemDeCompromisso(Compromisso compromisso, string nomeFicha)
    {
        return new ItemListaDiaria(
            TipoItemListaDiaria.Compromisso,
            compromisso.Id,
            compromisso.FichaId,
            nomeFicha,
            compromisso.Motivo,
            compromisso.DataHoraUtc,
            null,
            null,
            null,
            false);
    }
}
