// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Suporte;

/// <summary>
/// Dados fictícios da F008 gravados diretamente na base de dados de testes, sem depender dos endpoints de outras tarefas:
/// fichas com associações, tratamentos, tomas e compromissos.
/// </summary>
public static class DadosAgenda
{
    private static readonly CalendarioLisboa Calendario = new();
    private static readonly TimeOnly[] AsOito = { new(8, 0) };

    /// <summary>
    /// Instante UTC de um dia a uma hora de Lisboa.
    /// </summary>
    /// <param name="dia">Dia em hora de Lisboa.</param>
    /// <param name="hora">Hora de Lisboa.</param>
    /// <param name="minuto">Minuto.</param>
    /// <returns>O instante em UTC.</returns>
    public static DateTime Lisboa(DateOnly dia, int hora, int minuto = 0)
    {
        return Calendario.ParaUtc(dia, new TimeOnly(hora, minuto));
    }

    /// <summary>
    /// Cria uma ficha ativa com o Cuidador principal e, opcionalmente, pessoas associadas com o papel indicado.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="nome">Nome do utente.</param>
    /// <param name="principalId">Cuidador principal.</param>
    /// <param name="outros">Outras pessoas e o respetivo papel.</param>
    /// <returns>O identificador da ficha.</returns>
    public static Task<Guid> CriarFichaAsync(FabricaApi fabrica, string nome, Guid principalId, params (Guid Id, Papel Papel)[] outros)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        ArgumentNullException.ThrowIfNull(outros);
        return fabrica.ComContextoAsync(async contexto =>
        {
            var agora = DadosTeste.Agora();
            var ficha = new Ficha(nome, new DateOnly(1941, 5, 17), "+351 910 000 001", null, principalId, agora.AddDays(-60));
            ficha.AssociarCuidadorPrincipal(principalId, provisorio: false, agora.AddDays(-60));
            foreach (var (id, papel) in outros)
            {
                ficha.Associar(id, papel, agora.AddDays(-60));
            }

            contexto.Fichas.Add(ficha);
            await contexto.SaveChangesAsync();
            return ficha.Id;
        });
    }

    /// <summary>
    /// Arquiva uma ficha.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="fichaId">Ficha a arquivar.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public static Task ArquivarAsync(FabricaApi fabrica, Guid fichaId)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.ComContextoAsync(async contexto =>
        {
            var ficha = await contexto.Fichas.SingleAsync(f => f.Id == fichaId);
            ficha.Arquivar(DadosTeste.Agora());
            return await contexto.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Cria um tratamento da ficha (sem gerar tomas) e uma toma pendente no instante indicado.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="criadoPorId">Cuidador que regista.</param>
    /// <param name="previstaUtc">Instante previsto da toma.</param>
    /// <returns>A toma criada.</returns>
    public static Task<Toma> CriarTomaAsync(FabricaApi fabrica, Guid fichaId, Guid criadoPorId, DateTime previstaUtc)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.ComContextoAsync(async contexto =>
        {
            var tratamento = new Tratamento(
                fichaId,
                "Metformina",
                "500 mg",
                GrauImportancia.Moderado,
                DateOnly.FromDateTime(previstaUtc),
                null,
                AsOito,
                criadoPorId,
                DadosTeste.Agora());
            var toma = new Toma(tratamento.Id, previstaUtc, "Metformina", "500 mg", GrauImportancia.Moderado);
            contexto.Tratamentos.Add(tratamento);
            contexto.Tomas.Add(toma);
            await contexto.SaveChangesAsync();
            return toma;
        });
    }

    /// <summary>
    /// Cria um compromisso da ficha.
    /// </summary>
    /// <param name="fabrica">Fábrica da API.</param>
    /// <param name="fichaId">Ficha.</param>
    /// <param name="criadoPorId">Cuidador que regista.</param>
    /// <param name="dataHoraUtc">Data e hora, em UTC.</param>
    /// <param name="motivo">Motivo.</param>
    /// <returns>O compromisso criado.</returns>
    public static Task<Compromisso> CriarCompromissoAsync(
        FabricaApi fabrica,
        Guid fichaId,
        Guid criadoPorId,
        DateTime dataHoraUtc,
        string motivo = "Consulta de rotina")
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.ComContextoAsync(async contexto =>
        {
            var compromisso = new Compromisso(fichaId, dataHoraUtc, "Centro de Saúde Fictício", motivo, 1440, criadoPorId, DadosTeste.Agora());
            contexto.Compromissos.Add(compromisso);
            await contexto.SaveChangesAsync();
            return compromisso;
        });
    }
}
