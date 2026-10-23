// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;

namespace DoseCerta.UnitTests.Dominio;

/// <summary>
/// Testes unitários de <see cref="Tratamento"/> (RF-050, RF-055, RF-056, RF-060).
/// </summary>
public class TratamentoTestes
{
    private static readonly DateTime Agora = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeOnly Oito = new(8, 0);
    private static readonly TimeOnly Vinte = new(20, 0);
    private static readonly int[] Dias = { 2, 3, 4 };

    private readonly CalendarioLisboa calendario = new();

    /// <summary>
    /// RF-060 CA1: com as horas 08:00 e 20:00, de 02 a 04 de novembro, geram-se 6 tomas, que copiam o medicamento, a dose e o grau.
    /// </summary>
    [Fact]
    public void GeraUmaTomaPorHorarioEPorDia()
    {
        var tratamento = Novo(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4), Oito, Vinte);

        var tomas = tratamento.GerarTomas(Agora, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), this.calendario);

        // Em novembro, a hora de Lisboa coincide com UTC.
        var esperado = Dias
            .SelectMany(d => new[] { new DateTime(2026, 11, d, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, d, 20, 0, 0, DateTimeKind.Utc) });
        Assert.Equal(esperado, tomas.Select(t => t.PrevistaEmUtc));
        Assert.All(tomas, t =>
        {
            Assert.Equal(tratamento.Id, t.TratamentoId);
            Assert.Equal(EstadoToma.Pendente, t.Estado);
            Assert.Equal("Metformina", t.Medicamento);
            Assert.Equal("500 mg", t.Dose);
            Assert.Equal(GrauImportancia.Moderado, t.GrauImportancia);
        });
    }

    /// <summary>
    /// O intervalo pedido limita a geração: o início é inclusive e o fim é exclusive.
    /// </summary>
    [Fact]
    public void RespeitaOIntervaloPedido()
    {
        var tratamento = Novo(new DateOnly(2026, 11, 2), null, Oito, Vinte);

        var tomas = tratamento.GerarTomas(
            new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 3, 20, 0, 0, DateTimeKind.Utc),
            this.calendario);

        Assert.Equal(3, tomas.Count);
        Assert.Equal(new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc), tomas[0].PrevistaEmUtc);
        Assert.Equal(new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc), tomas[^1].PrevistaEmUtc);
    }

    /// <summary>
    /// RNF-047: no verão a hora de Lisboa é UTC+1; na mudança de hora, duas horas que dão o mesmo instante geram uma só toma.
    /// </summary>
    [Fact]
    public void SegueAHoraDeLisboaENaoDuplicaInstantes()
    {
        var verao = Novo(new DateOnly(2027, 7, 1), new DateOnly(2027, 7, 1), Oito);
        var mudanca = Novo(new DateOnly(2027, 3, 28), new DateOnly(2027, 3, 28), new TimeOnly(1, 30), new TimeOnly(2, 30));
        var de = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ate = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2027, 7, 1, 7, 0, 0, DateTimeKind.Utc), Assert.Single(verao.GerarTomas(de, ate, this.calendario)).PrevistaEmUtc);
        Assert.Equal(new DateTime(2027, 3, 28, 1, 30, 0, DateTimeKind.Utc), Assert.Single(mudanca.GerarTomas(de, ate, this.calendario)).PrevistaEmUtc);
    }

    /// <summary>
    /// RF-056: um tratamento terminado não gera tomas, não pode ser terminado de novo nem alterado (RF-055).
    /// </summary>
    [Fact]
    public void TerminadoNaoGeraNemMuda()
    {
        var tratamento = Novo(new DateOnly(2026, 11, 2), null, Oito);

        tratamento.Terminar(Agora);

        Assert.Equal(EstadoTratamento.Terminado, tratamento.Estado);
        Assert.Equal(Agora, tratamento.TerminadoEmUtc);
        Assert.Empty(tratamento.GerarTomas(Agora, Agora.AddDays(30), this.calendario));
        Assert.Equal(CodigosErro.TratamentoJaTerminado, Assert.Throws<DominioException>(() => tratamento.Terminar(Agora)).Codigo);
        var erro = Assert.Throws<DominioException>(() => tratamento.Atualizar("X", "Y", GrauImportancia.Leve, null, new[] { Oito }));
        Assert.Equal(CodigosErro.TratamentoTerminado, erro.Codigo);
    }

    /// <summary>
    /// RF-055: a atualização mantém os horários que continuam, acrescenta os novos e retira os que saem.
    /// </summary>
    [Fact]
    public void AtualizarSubstituiOsHorariosSemRecriarOsQueContinuam()
    {
        var tratamento = Novo(new DateOnly(2026, 11, 2), null, Oito, Vinte);
        var horarioDasOito = tratamento.Horarios.Single(h => h.HoraLocal == Oito);

        tratamento.Atualizar("Metformina", "850 mg", GrauImportancia.Grave, new DateOnly(2026, 12, 1), new[] { Oito, new TimeOnly(21, 0) });

        Assert.Equal("850 mg", tratamento.Dose);
        Assert.Equal(GrauImportancia.Grave, tratamento.GrauImportancia);
        Assert.Equal(new DateOnly(2026, 12, 1), tratamento.DataFim);
        Assert.Contains(horarioDasOito, tratamento.Horarios);
        Assert.Equal(new[] { Oito, new TimeOnly(21, 0) }, tratamento.Horarios.Select(h => h.HoraLocal).Order());
    }

    /// <summary>
    /// O domínio recusa um tratamento sem horas ou com a data de fim anterior à de início (RF-050 CA4).
    /// </summary>
    [Fact]
    public void RecusaDadosIncoerentes()
    {
        Assert.Throws<ArgumentException>(() => Novo(new DateOnly(2026, 11, 2), null));
        Assert.Throws<ArgumentException>(() => Novo(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 1), Oito));
    }

    /// <summary>
    /// A data de fim fica ultrapassada no dia seguinte, só num tratamento em curso.
    /// </summary>
    [Fact]
    public void DataFimUltrapassadaSoDepoisDoUltimoDia()
    {
        var comFim = Novo(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4), Oito);
        var semFim = Novo(new DateOnly(2026, 11, 2), null, Oito);

        Assert.False(comFim.DataFimUltrapassada(new DateOnly(2026, 11, 4)));
        Assert.True(comFim.DataFimUltrapassada(new DateOnly(2026, 11, 5)));
        Assert.False(semFim.DataFimUltrapassada(new DateOnly(2030, 1, 1)));
        comFim.Terminar(Agora);
        Assert.False(comFim.DataFimUltrapassada(new DateOnly(2026, 11, 5)));
    }

    /// <summary>
    /// RF-052: só os documentos da categoria receita médica são receitas.
    /// </summary>
    [Fact]
    public void DocumentoEReceitaSoNaCategoriaReceitaMedica()
    {
        var receita = new Documento(Guid.NewGuid(), Guid.NewGuid(), CategoriaDocumento.ReceitaMedica, "r.pdf", "a.pdf", FormatoDocumento.PDF, 10, Guid.NewGuid(), Agora);
        var relatorio = new Documento(Guid.NewGuid(), null, CategoriaDocumento.RelatorioClinico, "r.pdf", "b.pdf", FormatoDocumento.PDF, 10, Guid.NewGuid(), Agora);

        Assert.True(receita.EReceita());
        Assert.False(relatorio.EReceita());
    }

    private static Tratamento Novo(DateOnly inicio, DateOnly? fim, params TimeOnly[] horas)
    {
        return new Tratamento(Guid.NewGuid(), "Metformina", "500 mg", GrauImportancia.Moderado, inicio, fim, horas, Guid.NewGuid(), Agora);
    }
}
