// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Aplicacao.Notificacoes;

/// <summary>
/// Testes unitários de <see cref="TextosNotificacao"/> (RNF-020, RNF-029, OP-51).
/// </summary>
public class TextosNotificacaoTestes
{
    /// <summary>
    /// A mensagem da toma por confirmar segue o exemplo de OP-51 e cabe nos 400 caracteres de <c>Notificacao.Mensagem</c>,
    /// mesmo com o medicamento (120) e o nome (150) mais longos.
    /// </summary>
    [Fact]
    public void MensagemDaTomaComoNoExemploDaApi()
    {
        Assert.Equal(
            "A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.",
            TextosNotificacao.TomaPorConfirmar("Insulina", "Maria Fictícia Santos", "10:00"));
        Assert.True(TextosNotificacao.TomaPorConfirmar(new string('m', 120), new string('n', 150), "23:59").Length <= 400);
    }

    /// <summary>
    /// I056 (RF-117 CA1): a mensagem do aviso de compromisso tem o nome do utente, o motivo, o local, a data e a hora.
    /// </summary>
    [Fact]
    public void MensagemDoCompromissoTemOsCincoDados()
    {
        Assert.Equal(
            "Compromisso de Maria Fictícia Santos: Consulta de cardiologia, em Centro de Saúde Fictício, a 12/11/2026 às 10:30.",
            TextosNotificacao.ProximoCompromisso("Consulta de cardiologia", "Centro de Saúde Fictício", "Maria Fictícia Santos", "12/11/2026", "10:30"));
    }

    /// <summary>
    /// I056 (RNF-020): o assunto do aviso de compromisso não tem motivo, local nem nome do utente.
    /// </summary>
    [Fact]
    public void AssuntoDoCompromissoSemDadosDeSaude()
    {
        var assunto = TextosNotificacao.AssuntoEmail(TipoNotificacao.ProximoCompromisso);

        Assert.Equal("DoseCerta: compromisso próximo", assunto);
        Assert.DoesNotContain("Consulta", assunto, StringComparison.Ordinal);
    }

    /// <summary>
    /// I056: com o nome (150), o local (150) e o motivo (250) mais longos, a mensagem fica com 400 caracteres; o motivo e o
    /// local são encurtados com «…» e o nome, a data e a hora ficam inteiros.
    /// </summary>
    [Fact]
    public void MensagemDoCompromissoCabeNos400Caracteres()
    {
        var nome = new string('n', 150);

        var mensagem = TextosNotificacao.ProximoCompromisso(new string('m', 250), new string('l', 150), nome, "12/11/2026", "10:30");

        Assert.Equal(TextosNotificacao.MensagemMaxima, mensagem.Length);
        Assert.Contains(nome, mensagem, StringComparison.Ordinal);
        Assert.Contains("m…, em", mensagem, StringComparison.Ordinal);
        Assert.EndsWith("a 12/11/2026 às 10:30.", mensagem, StringComparison.Ordinal);
    }

    /// <summary>
    /// I056: se o local for curto, só o motivo é encurtado; se couber tudo, nada é cortado.
    /// </summary>
    [Fact]
    public void MensagemDoCompromissoSoEncurtaOQueFalta()
    {
        var soMotivo = TextosNotificacao.ProximoCompromisso(new string('m', 250), "Hospital", new string('n', 150), "12/11/2026", "10:30");
        var inteira = TextosNotificacao.ProximoCompromisso(new string('m', 100), new string('l', 100), "Rui", "12/11/2026", "10:30");

        Assert.Equal(TextosNotificacao.MensagemMaxima, soMotivo.Length);
        Assert.Contains(", em Hospital, a", soMotivo, StringComparison.Ordinal);
        Assert.DoesNotContain("…", inteira, StringComparison.Ordinal);
        Assert.Contains(new string('m', 100), inteira, StringComparison.Ordinal);
    }

    /// <summary>
    /// O assunto do e-mail depende do tipo, não tem dados de saúde e cabe nos 150 caracteres de <c>EnvioEmail.Assunto</c>; os
    /// tipos sem assunto próprio usam o genérico.
    /// </summary>
    /// <param name="tipo">Tipo da notificação.</param>
    /// <param name="esperado">Assunto esperado.</param>
    [Theory]
    [InlineData(TipoNotificacao.TomaEmAtraso, TextosNotificacao.AssuntoTomaEmAtraso)]
    [InlineData(TipoNotificacao.TomaGraveFamiliares, TextosNotificacao.AssuntoTomaGraveFamiliares)]
    [InlineData(TipoNotificacao.ConviteRecebido, TextosConvite.Assunto)]
    [InlineData(TipoNotificacao.NovaOcorrencia, TextosNotificacao.AssuntoGenerico)]
    [InlineData(TipoNotificacao.ProximoCompromisso, TextosNotificacao.AssuntoProximoCompromisso)]
    public void AssuntoDoEmailPorTipo(TipoNotificacao tipo, string esperado)
    {
        var assunto = TextosNotificacao.AssuntoEmail(tipo);

        Assert.Equal(esperado, assunto);
        Assert.True(assunto.Length <= 150);
    }

    /// <summary>
    /// O corpo do e-mail repete a mensagem da notificação, indica onde a consultar e quando foi criada.
    /// </summary>
    [Fact]
    public void CorpoDoEmailTemAMensagemEAData()
    {
        Assert.Equal(
            "Mensagem da notificação.\n\nPode consultar esta notificação no histórico de notificações do DoseCerta.\n\n"
            + "Notificação de 07/10/2026 11:00.",
            TextosNotificacao.CorpoEmail("Mensagem da notificação.", "07/10/2026 11:00"));
    }
}
