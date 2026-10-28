// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Abstracoes;
using DoseCerta.Application.Comum;
using DoseCerta.Application.Fichas;
using DoseCerta.Application.Repositorios;
using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Agenda;

/// <summary>
/// Agenda do Utilizador: reúne os compromissos e as tomas previstas das fichas ativas em que é Cuidador ou Familiar, numa
/// vista diária, semanal ou mensal em hora de Lisboa, só para consulta (F008, RF-083 a RF-089, RNF-012, RNF-047).
/// </summary>
public sealed class AgendaService
{
    private readonly AutorizacaoFichaService autorizacao;
    private readonly ICompromissoRepositorio compromissos;
    private readonly ITomaRepositorio tomas;
    private readonly IRelogio relogio;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgendaService"/> class.
    /// </summary>
    /// <param name="autorizacao">Verificação das fichas do Utilizador.</param>
    /// <param name="compromissos">Repositório dos compromissos.</param>
    /// <param name="tomas">Repositório das tomas.</param>
    /// <param name="relogio">Relógio do sistema, com o calendário de Lisboa.</param>
    public AgendaService(
        AutorizacaoFichaService autorizacao,
        ICompromissoRepositorio compromissos,
        ITomaRepositorio tomas,
        IRelogio relogio)
    {
        this.autorizacao = autorizacao;
        this.compromissos = compromissos;
        this.tomas = tomas;
        this.relogio = relogio;
    }

    /// <summary>
    /// Converte o texto da vista pedida (<c>dia</c>, <c>semana</c> ou <c>mes</c>).
    /// </summary>
    /// <param name="texto">Texto recebido.</param>
    /// <returns>A vista, ou <see langword="null"/> se o texto não for uma das três.</returns>
    public static VistaAgenda? LerVista(string? texto)
    {
        return ValidadorDados.Normalizar(texto)?.ToUpperInvariant() switch
        {
            "DIA" => VistaAgenda.Dia,
            "SEMANA" => VistaAgenda.Semana,
            "MES" => VistaAgenda.Mes,
            _ => null,
        };
    }

    /// <summary>
    /// OP-45: devolve os eventos do intervalo da vista, por ordem do instante. Os compromissos aparecem na data registada; as
    /// tomas só existem na janela móvel de 30 dias. As fichas arquivadas não aparecem e, sem fichas, a lista é vazia (RF-083).
    /// </summary>
    /// <param name="utilizadorId">Utilizador autenticado.</param>
    /// <param name="vista">Vista pedida: <c>dia</c>, <c>semana</c> ou <c>mes</c>; obrigatória.</param>
    /// <param name="data">Data que define o intervalo, em hora de Lisboa; obrigatória.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>Os eventos da agenda.</returns>
    /// <exception cref="ValidacaoException">Se a vista ou a data faltarem ou forem inválidas.</exception>
    public async Task<IReadOnlyList<ItemAgenda>> ConsultarAsync(
        Guid utilizadorId,
        string? vista,
        DateOnly? data,
        CancellationToken cancelamento)
    {
        var vistaPedida = LerVista(vista);
        var validador = new ValidadorDados();
        if (vistaPedida is null)
        {
            validador.Erro("vista", "É obrigatória e tem de ser dia, semana ou mes.");
        }

        if (data is null)
        {
            validador.Erro("data", "É obrigatória, no formato AAAA-MM-DD.");
        }

        validador.LancarSeInvalido();

        var fichas = await this.autorizacao.IdentificadoresDeFichasAtivasAsync(utilizadorId, null, cancelamento).ConfigureAwait(false);
        if (fichas.Count == 0)
        {
            return Array.Empty<ItemAgenda>();
        }

        var (deUtc, ateUtc) = this.Intervalo(vistaPedida!.Value, data!.Value);
        var ids = fichas.Keys.ToList();
        var listaCompromissos = await this.compromissos.DoPeriodoDasFichasAsync(ids, deUtc, ateUtc, cancelamento).ConfigureAwait(false);
        var listaTomas = await this.tomas.DoPeriodoDasFichasAsync(ids, deUtc, ateUtc, cancelamento).ConfigureAwait(false);

        var itens = new List<ItemAgenda>(listaCompromissos.Count + listaTomas.Count);
        foreach (var (fichaId, toma) in listaTomas)
        {
            var acesso = fichas[fichaId];
            itens.Add(new ItemAgenda(
                toma.Id,
                TipoItemAgenda.Toma,
                fichaId,
                acesso.Ficha.NomeCompleto,
                $"{toma.Medicamento} {toma.Dose}",
                toma.PrevistaEmUtc,
                toma.Estado,
                toma.Estado == EstadoToma.Pendente && acesso.Associacao.EhCuidador()));
        }

        foreach (var compromisso in listaCompromissos)
        {
            itens.Add(new ItemAgenda(
                compromisso.Id,
                TipoItemAgenda.Compromisso,
                compromisso.FichaId,
                fichas[compromisso.FichaId].Ficha.NomeCompleto,
                compromisso.Motivo,
                compromisso.DataHoraUtc,
                null,
                false));
        }

        return itens
            .OrderBy(i => i.InicioUtc)
            .ThenBy(i => i.NomeFicha, StringComparer.Ordinal)
            .ThenBy(i => i.Tipo)
            .ThenBy(i => i.Id)
            .ToList();
    }

    // Limites em hora de Lisboa, convertidos para UTC: um dia pode ter 23 ou 25 horas nas mudanças de hora (RNF-047).
    private (DateTime DeUtc, DateTime AteUtc) Intervalo(VistaAgenda vista, DateOnly data)
    {
        var calendario = this.relogio.Calendario();
        return vista switch
        {
            VistaAgenda.Dia => (calendario.InicioDoDiaUtc(data), calendario.InicioDoDiaSeguinteUtc(data)),
            VistaAgenda.Semana => (calendario.InicioDaSemanaUtc(data), calendario.InicioDaSemanaUtc(data.AddDays(7))),
            _ => (calendario.InicioDoMesUtc(data), calendario.InicioDoMesSeguinteUtc(data)),
        };
    }
}
